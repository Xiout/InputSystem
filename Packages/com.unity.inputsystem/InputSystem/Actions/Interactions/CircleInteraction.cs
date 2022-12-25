using System.ComponentModel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Scripting;
using System.Collections.Generic;
using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
using UnityEditor;
#endif

namespace UnityEngine.InputSystem.Interactions
{
    /// <summary>
    /// Performs the action if the control is pressed and held for at least the
    /// set duration (which defaults to <see cref="InputSettings.defaultHoldTime"/>).
    /// </summary>
    /// <remarks>
    /// The action is started when the control is pressed. If the control is released before the
    /// set <see cref="duration"/>, the action is canceled. As soon as the hold time is reached,
    /// the action performs. The action then stays performed until the control is released, at
    /// which point the action cancels.
    ///
    /// <example>
    /// <code>
    /// // Action that requires A button on gamepad to be held for half a second.
    /// var action = new InputAction(binding: "&lt;Gamepad&gt;/buttonSouth", interactions: "hold(duration=0.5)");
    /// </code>
    /// </example>
    /// </remarks>

    #if UNITY_EDITOR
    //Register interaction to the Input Manager
    [InitializeOnLoad]
    #endif

    [DisplayName("Circle")]
    public class CircleInteraction : IInputInteraction//<Vector2>
    {
        /// <summary>
        /// Duration in seconds that the control must be pressed for the hold to register.
        /// </summary>
        /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="InputSettings.defaultHoldTime"/> is used.
        ///
        /// Duration is expressed in real time and measured against the timestamps of input events
        /// (<see cref="LowLevel.InputEvent.time"/>) not against game time (<see cref="Time.time"/>).
        /// </remarks>
        public float duration;

        /// <summary>
        /// Magnitude threshold that must be crossed by an actuated control for the control to
        /// be considered pressed.
        /// </summary>
        /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="InputSettings.defaultButtonPressPoint"/> is used instead.
        /// </remarks>
        /// <seealso cref="InputControl.EvaluateMagnitude()"/>
        public float pressPoint;

        private float durationOrDefault => duration > 0.0 ? duration : InputSystem.settings.defaultHoldTime;
        private float pressPointOrDefault => pressPoint > 0.0 ? pressPoint : ButtonControl.s_GlobalDefaultButtonPressPoint;

        private double m_TimePressed;

        /// <summary>
        /// List build when the interaction is started and register all the position of the device (mouse). This position will then be analysed to check if it form a circle
        /// </summary>
        private List<Vector2> m_ListPositionsOverTime;

        private bool m_hasStarted;

        private InputInteractionContext latestContext;

        private void OnUpdate()
        {
            var listControls = latestContext.control.device.allControls.ToList();
            Vector2Control controlPosition = listControls.Find(ctr => ctr.valueType == typeof(Vector2)) as Vector2Control;

            Debug.Log(controlPosition.x.ReadValue() + " , " + controlPosition.y.ReadValue());

        }

       /// <inheritdoc />
       public void Process(ref InputInteractionContext context)
        {
            Debug.Log("PROCESS");
            m_hasStarted = false;

            latestContext = context;

            //Get control from context that store the mouse or stick position 
            //TODO : find a safer version that find the right element event the device has multiple control returning Vector2 as value
            //or use a Dictionnary of Control/List<Vector2> to trqck qll control using vector 2 and check if at least one is making a circle
            var listControls = latestContext.control.device.allControls.ToList();
            Vector2Control controlPosition = listControls.Find(ctr => ctr.valueType == typeof(Vector2)) as Vector2Control;

            switch (latestContext.phase)
            {
                case InputActionPhase.Waiting:
                    Debug.Log("WAITING");
                    if (context.ControlIsActuated(pressPointOrDefault))
                    {
                        m_hasStarted = true;

                        if (m_ListPositionsOverTime == null)
                        {
                            m_ListPositionsOverTime = new List<Vector2>();
                        }
                    }

                    if (m_hasStarted)
                    {
                        if (!latestContext.ControlIsActuated())
                        {
                            Debug.Log("WAITING : STOP USING");
                            latestContext.Canceled();
                            m_ListPositionsOverTime = null;
                        }


                        m_ListPositionsOverTime.Add(new Vector2(controlPosition.x.ReadValue(), controlPosition.y.ReadValue()));
                        if (latestContext.time - m_TimePressed >= durationOrDefault)
                        {
                            latestContext.Started();
                        }
                    }
                    break;

                case InputActionPhase.Started:
                    Debug.Log("START USING");
                    //TODO : Get value from current controlPosition and add it to m_ListPositionsOverTime
                   
                    m_ListPositionsOverTime.Add(new Vector2(controlPosition.x.ReadValue(), controlPosition.y.ReadValue()));
                    //Debug.Log(controlPosition.x.ReadValue() + " , " + controlPosition.y.ReadValue()+" size: "+ m_ListPositionsOverTime.Count);

                    //TODO : Condition to check if all registered positions form a circle
                    if(IsACircle(m_ListPositionsOverTime))
                    {
                        latestContext.PerformedAndStayPerformed();
                    }

                    // ControlIsActuted indicate is the control is currently used (ie. button pressed, stick not in its initial position,...)
                    if (!latestContext.ControlIsActuated())
                    {
                        Debug.Log("STARTED : STOP USING");
                        latestContext.Canceled();
                        m_hasStarted = false;
                        m_ListPositionsOverTime = null;
                    }
                    break;

                case InputActionPhase.Performed:
                    if (!latestContext.ControlIsActuated(pressPointOrDefault))
                    {
                        Debug.Log("PERFORMED : STOP USING");
                        latestContext.Canceled();
                        m_hasStarted = false;
                        m_ListPositionsOverTime = null;
                    }   
                    break;
            }
        }

        private bool IsACircle(List<Vector2> points)
        {
            //TODO find center
            //TODO check is all points are at the same distance from the center

            //TEMPORARY : if the first and the last point are the same, then it is a circle
            return (points.Count > 2 && points[0] == points[points.Count - 1] && points[0] != points[1]);
        }

        /// <inheritdoc />
        public void Reset()
        {
            m_TimePressed = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void RegisterInteraction()
        {
            if (InputSystem.TryGetInteraction("Circle") == null)
            {
                InputSystem.RegisterInteraction<CircleInteraction>();
            }
        }

        //Constructor will be called by our Editor [InitializeOnLoad] attribute
        static CircleInteraction()
        {
            RegisterInteraction();
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// UI that is displayed when editing <see cref="HoldInteraction"/> in the editor.
    /// </summary>
    internal class CircleInteractionEditor : InputParameterEditor<HoldInteraction>
    {
        protected override void OnEnable()
        {
            m_PressPointSetting.Initialize("Press Point",
                "Float value that an axis control has to cross for it to be considered pressed.",
                "Default Button Press Point",
                () => target.pressPoint, v => target.pressPoint = v, () => ButtonControl.s_GlobalDefaultButtonPressPoint);
            m_DurationSetting.Initialize("Hold Time",
                "Time (in seconds) that a control has to be held in order for it to register as a hold.",
                "Default Hold Time",
                () => target.duration, x => target.duration = x, () => InputSystem.settings.defaultHoldTime);
        }

        public override void OnGUI()
        {
            m_PressPointSetting.OnGUI();
            m_DurationSetting.OnGUI();
        }

        private CustomOrDefaultSetting m_PressPointSetting;
        private CustomOrDefaultSetting m_DurationSetting;
    }
    #endif
}
