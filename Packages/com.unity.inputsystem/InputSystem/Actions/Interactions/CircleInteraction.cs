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
        //public float duration;

        /// <summary>
        /// Magnitude threshold that must be crossed by an actuated control for the control to
        /// be considered pressed.
        /// </summary>
        /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="InputSettings.defaultButtonPressPoint"/> is used instead.
        /// </remarks>
        /// <seealso cref="InputControl.EvaluateMagnitude()"/>
        //public float pressPoint;

        /// <summary>
        /// Maximum offset allowed between a point and its estimation to be consider correct.
        /// </summary> 
        public float accuracyOffset;

        //private float durationOrDefault => duration > 0.0 ? duration : InputSystem.settings.defaultHoldTime;
        //private float pressPointOrDefault => pressPoint > 0.0 ? pressPoint : ButtonControl.s_GlobalDefaultButtonPressPoint;

        private List<ButtonControl> m_buttonControls;
        private int m_indexButtonActuated = -1;

        /// <summary>
        /// List build when the interaction is started and register all the position of the device (mouse). This position will then be analysed to check if it form a circle
        /// </summary>
        private List<Vector2> m_ListPositionsOverTime;

       /// <inheritdoc />
       public void Process(ref InputInteractionContext context)
        {
            //Get all control button in m_ListButtonControl 
            //Note that the list is build backward because the first button of the Mouse (Press) is true only when starting pressing a button
            m_buttonControls = new List<ButtonControl>();
            var listControls = context.control.device.allControls.ToList();
            for(int i=0; i<listControls.Count; ++i)
            {
                var ctr = listControls[i];
                ButtonControl ctrButton = ctr as ButtonControl;
                if(ctrButton != null)
                {
                    m_buttonControls.Insert(0,ctrButton);
                }
            }

            Vector2 value = (context.control as InputControl<Vector2>).ReadUnprocessedValue();

            switch (context.phase)
            {
                case InputActionPhase.Waiting:
                    Debug.Log($"WAIT");

                    if (!context.ControlIsActuated())
                    {
                        break;
                    }

                    for(int i=0; i<m_buttonControls.Count; ++i)
                    {
                        //check if any buttonControl is actuated, if yes the index is kept in m_indexButtonActuated and the context is started
                        if (m_buttonControls[i].IsActuated())
                        {
                            m_indexButtonActuated = i;
                            Debug.Log($"START index:{m_indexButtonActuated}");
                            context.Started();
                            if (m_ListPositionsOverTime == null)
                            {
                                m_ListPositionsOverTime = new List<Vector2>();
                            }

                            m_ListPositionsOverTime.Add(value);
                            break;
                        }
                    }

                    break;
                case InputActionPhase.Started:
                    Debug.Log("STARTED");
                    if (context.ControlIsActuated())
                    {
                        m_ListPositionsOverTime.Add(value);
                        //Check if the list of point collected over time form a circle, if yes the action is performed
                        if (IsACircle(m_ListPositionsOverTime))
                        {
                            Debug.Log("PERFORM");
                            context.PerformedAndStayPerformed();
                            break;
                        }

                        //TODO : Implement "CanBeCircle" to define if the points form an incomplete circle. if send false, call context.Canceled();
                    }

                    //if the control binded or the button control used to start the action are no longer actuated, the action is canceled
                    if (!context.ControlIsActuated() || m_indexButtonActuated < 0 || !m_buttonControls[m_indexButtonActuated].IsActuated())
                    {
                        Debug.Log($"CANCEL IN STARTED");
                        context.Canceled();
                        Reset();
                    }
                    break;

                case InputActionPhase.Performed:
                    Debug.Log("PERFORMED");
                    //The action stays performed as long as the control binded is actuated and the button control used is still held
                    if (!context.ControlIsActuated() || m_indexButtonActuated < 0 || !m_buttonControls[m_indexButtonActuated].IsActuated())
                    {
                        Debug.Log("CANCEL IN PERFORMED");
                        context.Canceled();
                        Reset();
                    }
                    break;
            }
        }

        /// <inheritdoc />
        public void Reset()
        {
            m_indexButtonActuated = -1;
            m_ListPositionsOverTime = new List<Vector2>();
        }

        private bool IsACircle(List<Vector2> points)
        {
            //TODO find center
            //TODO check is all points are at the same distance from the center

            //TEMPORARY : if the first and the last point are the same, then it is a circle

            if(points.Count <= 2)
            {
                return false;
            }

            return (Math.Abs(points[0].x - points[points.Count - 1].x) <= accuracyOffset && Math.Abs(points[0].y - points[points.Count - 1].y) <= accuracyOffset);
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
