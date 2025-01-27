using System.ComponentModel;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Scripting;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.InputSystem.Utilities;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;
using UnityEditor;
#endif

namespace UnityEngine.InputSystem.Interactions
{
    /// <summary>
    /// Performs the action if the control is pressed and held for at least the
    /// set minimal duration (which defaults to <see cref="InputSettings.defaultHoldTime"/>) 
    /// but less than the maximal duration (which defaults to <see cref="InputSettings.defaultCircleTimeMax"/>
    /// and form a circle.
    /// </summary>
    /// <remarks>
    /// The action is started when a control button of the same device is pressed. If the control is released before the
    /// set <see cref="durationMin"/>, the action is canceled.
    /// When the hold time is reached, calculate at each value update if the list of points collected form a circle. 
    /// If yes, the action performs. The action then stays performed until the control is released, at
    /// which point the action cancels.
    /// If after the set <see cref="durationMax"/>, the points collected still does not form a circle, the action is canceled.

    #if UNITY_EDITOR
    //Register interaction to the Input Manager
    [InitializeOnLoad]
    #endif

    [DisplayName("Circle")]
    public class CircleInteraction : IInputInteraction//<Vector2>
    {
        /// <summary>
        /// Duration in seconds that the control must be pressed at least to register.
        /// </summary>
        /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="InputSettings.defaultHoldTime"/> is used.
        ///
        /// Duration is expressed in real time and measured against the timestamps of input events
        /// (<see cref="LowLevel.InputEvent.time"/>) not against game time (<see cref="Time.time"/>).
        /// </remarks>
        public float durationMin;

        /// <summary>
        /// Duration in seconds under which the circle must be performed to be register. 
        /// </summary>
        /// /// <remarks>
        /// If this is less than or equal to 0 (the default), <see cref="InputSettings.defaultCircleTimeMax"/> is used.
        ///
        /// Duration is expressed in real time and measured against the timestamps of input events
        /// (<see cref="LowLevel.InputEvent.time"/>) not against game time (<see cref="Time.time"/>).
        /// </remarks>
        public float durationMax;

        /// <summary>
        /// Exactness requiered for the circle to be register (0-100) 
        /// </summary> 
        /// <remarks>
        /// The maximal offset allowed between a point and its estimation to be considered correct will be calculated using this parameter and the distance between the furthest points.
        /// To have optimal result, the value should be greater than 60
        /// </remarks>
        public float accuracyPercent;

        private bool isMouse;

        /// <summary>
        /// Define which method is used to calculate if the list of points aquiered during the Interaction Process form a circle
        /// </summary>
        /// <remarks>
        /// <see cref="GeometryHelp.CircleMethod.MouseFurthestPoints"/> give the best results
        /// </remarks>
        public GeometryHelp.CircleMethod circleMethod;

        private float durationMinOrDefault => durationMin > 0.0 ? durationMin : InputSystem.settings.defaultHoldTime;
        private float durationMaxOrDefault => durationMax > 0.0 ? durationMax : InputSystem.settings.defaultCircleTimeMax;

        private float accuracyPercentOrDefault => (accuracyPercent > 0.0 && accuracyPercent <= 100.0) ? accuracyPercent : InputSystem.settings.defaultAccuracyPercent;

        private List<ButtonControl> m_buttonControls;
        private int m_indexButtonActuated = -1;

        private double m_TimePressed;

        /// <summary>
        /// List build when the interaction is started and register all the position of the device (mouse). This position will then be analysed to check if it form a circle
        /// </summary>
        private List<Vector2> m_ListPositionsOverTime;

       /// <inheritdoc />
       public void Process(ref InputInteractionContext context)
        {
            isMouse = context.control.device.description.deviceClass == "Mouse";

            if (isMouse)
            {
                //Get all control button in m_ListButtonControl 
                //Note that the list is build backward because the first button of the Mouse (Press) is true only when starting pressing a button
                m_buttonControls = new List<ButtonControl>();
                var listControls = context.control.device.allControls.ToList();
                for (int i = 0; i < listControls.Count; ++i)
                {
                    var ctr = listControls[i];
                    ButtonControl ctrButton = ctr as ButtonControl;
                    if (ctrButton != null)
                    {
                        m_buttonControls.Insert(0, ctrButton);
                    }
                }
            }

            Vector2 value = (context.control as InputControl<Vector2>).ReadValue();

            switch (context.phase)
            {
                case InputActionPhase.Waiting:
                    if (!context.ControlIsActuated())
                    {
                        break;
                    }

                    if (isMouse)
                    {
                        for (int i = 0; i < m_buttonControls.Count; ++i)
                        {
                            //check if any buttonControl is actuated, if yes the index is kept in m_indexButtonActuated and the context is started
                            if (m_buttonControls[i].IsActuated())
                            {
                                m_indexButtonActuated = i;
                                break;
                            }
                        }
                    }
                    if (IsHold(context))
                    {
                        m_TimePressed = context.time;
                        context.Started();
                        if (m_ListPositionsOverTime == null)
                        {
                            m_ListPositionsOverTime = new List<Vector2>();
                        }

                        m_ListPositionsOverTime.Add(value);
                    }
                    
                    break;
                case InputActionPhase.Started:
                    if (context.ControlIsActuated())
                    {
                        if (context.time - m_TimePressed > durationMaxOrDefault)
                        {
                            //Time expired, the action is canceled
                            context.Canceled();
                            Reset();
                            break;
                        }

                        m_ListPositionsOverTime.Add(value);

                        if (context.time - m_TimePressed >= durationMinOrDefault)
                        {
                            //Check if the list of point collected over time form a circle, if yes the action is performed
                            if (GeometryHelp.IsCircle(m_ListPositionsOverTime, accuracyPercentOrDefault, circleMethod))
                            {
                                context.PerformedAndStayPerformed();
                                break;
                            }
                        }
                    }

                    //if the control binded or the button control used to start the action are no longer actuated, the action is canceled
                    if (!IsHold(context))
                    {
                        context.Canceled();
                        Reset();
                    }
                    break;

                case InputActionPhase.Performed:
                    //The action stays performed as long as the control binded is actuated and the button control used is still held
                    if (!IsHold(context))
                    {
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

        /// <summary>
        /// Check is the control is actuated.
        /// If using the mouse, check if a button of the mouse is also pressed at the same time.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private bool IsHold(InputInteractionContext context)
        {
            //If using the mouse, since mouse position is always sending actuated as long as the game is running,
            //we need to use an extra control of the same device to define if the control is hold
            if (isMouse)
            {
                if(m_indexButtonActuated < 0)
                {
                    return false;
                }

                if(m_buttonControls == null || m_buttonControls.Count <= 0)
                {
                    return false;
                }

                if (!m_buttonControls[m_indexButtonActuated].IsActuated())
                {
                    return false;
                }
            }

            return context.ControlIsActuated();
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
    /// UI that is displayed when editing <see cref="CircleInteraction"/> in the editor.
    /// </summary>
    internal class CircleInteractionEditor : InputParameterEditor<CircleInteraction>
    {
        protected override void OnEnable()
        {
            //m_PressPointSetting.Initialize("Press Point",
            //    "Float value that an axis control has to cross for it to be considered pressed.",
            //    "Default Button Press Point",
            //    () => target.pressPoint, v => target.pressPoint = v, () => ButtonControl.s_GlobalDefaultButtonPressPoint);
            m_MinDurationSetting.Initialize("Minumum Hold Time",
                "Time (in seconds) that a control has to be held in order to be able to register as a circle.",
                "Default Hold Time",
                () => target.durationMin, x => target.durationMin = x, () => InputSystem.settings.defaultHoldTime);

            m_MaxDurationSetting.Initialize("Maximum Hold Time",
               "Time (in seconds) after which, if the control is still held, the action will not register.",
               "Default Circle Time Max",
               () => target.durationMax, x => target.durationMax = x, () => InputSystem.settings.defaultCircleTimeMax);

            m_AccuracyPercentSetting.Initialize("Accuracy",
              "Exactness required for the circle to be register(0-100). For optimal results, consider setting a value between 50 and 80%",
              "Accuracy Percent",
              () => target.accuracyPercent, x => target.accuracyPercent = x, () => InputSystem.settings.defaultAccuracyPercent);

            m_CircleMethodSetting.Initialize("Circle Algorithm",
                "Method to be used to calculate if the list of points acquired during the Interaction Process forms a circle.",
                "Circle Method",
                () => target.circleMethod, x => target.circleMethod = x, Enum.GetNames(typeof(GeometryHelp.CircleMethod)));
        }

        public override void OnGUI()
        {
            m_MinDurationSetting.OnGUI();
            m_MaxDurationSetting.OnGUI();
            m_AccuracyPercentSetting.OnGUI();
            m_CircleMethodSetting.OnGUI();
        }

        private CustomOrDefaultSetting m_MinDurationSetting;
        private CustomOrDefaultSetting m_MaxDurationSetting;
        private CustomOrDefaultSetting m_AccuracyPercentSetting;
        private EnumSetting<GeometryHelp.CircleMethod> m_CircleMethodSetting;
        //private InputParameterEditor m_CircleMethodSetting;
    }
    #endif
}
