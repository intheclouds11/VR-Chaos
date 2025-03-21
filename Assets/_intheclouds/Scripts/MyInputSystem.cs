using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.OpenXR.Input;

public class MyInputSystem : MonoBehaviour
{
    public static MyInputActions MyInputActions;
    public static MyInputSystem Instance;

    private InputDevice leftInputDevice;
    private InputDevice rightInputDevice;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Cleanup()
    {
        if (MyInputActions != null)
        {
            MyInputActions.Disable();
            MyInputActions.Dispose();
            MyInputActions = null;
        }
    }

    private void Awake()
    {
        Instance = this;

        if (MyInputActions == null)
        {
            MyInputActions = new MyInputActions();
            MyInputActions.Enable();
        }

        InputSystem.onDeviceChange += OnDeviceChanged;
    }


    private void OnDeviceChanged(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added)
        {
            if (device.usages.Contains(CommonUsages.LeftHand))
            {
                leftInputDevice = device;
            }
            else if (device.usages.Contains(CommonUsages.RightHand))
            {
                rightInputDevice = device;
            }
        }
    }

    public bool IsGripActive(bool leftHand)
    {
        float grip = 0f;
        if (leftHand)
        {
            grip = MyInputActions.XRILeftInteraction.Grip.ReadValue<float>();
        }
        else
        {
            grip = MyInputActions.XRIRightInteraction.Grip.ReadValue<float>();
        }

        return grip >= 0.75f;
    }
    
    public bool WasGripActivated(bool leftHand)
    {
        bool activated;
        if (leftHand)
        {
            activated = MyInputActions.XRILeftInteraction.Grip.WasPressedThisFrame();
        }
        else
        {
            activated = MyInputActions.XRIRightInteraction.Grip.WasPressedThisFrame();
        }

        return activated;
    }

    public bool IsTriggerActive(bool leftHand)
    {
        float trigger = 0f;
        if (leftHand)
        {
            trigger = MyInputActions.XRILeftInteraction.Trigger.ReadValue<float>();
        }
        else
        {
            trigger = MyInputActions.XRIRightInteraction.Trigger.ReadValue<float>();
        }

        return trigger >= 0.75f;
    }
    
    public bool WasTriggerActivated(bool leftHand)
    {
        bool activated;
        if (leftHand)
        {
            activated = MyInputActions.XRILeftInteraction.Trigger.WasPressedThisFrame();
        }
        else
        {
            activated = MyInputActions.XRIRightInteraction.Trigger.WasPressedThisFrame();
        }

        return activated;
    }

    #region DesktopMode

    public Vector2 GetDesktopTranslation()
    {
        return MyInputActions.DesktopMode.Translate.ReadValue<Vector2>();
    }

    public bool WasDesktopJumpActivated()
    {
        return MyInputActions.DesktopMode.Jump.WasPressedThisFrame();
    }

    #endregion

    public void Vibrate(bool leftHand, float amplitude, float duration = 1, float frequency = 1)
    {
        var action = leftHand ? MyInputActions.XRILeft.HapticDevice : MyInputActions.XRIRight.HapticDevice;
        var inputDevice = leftHand ? leftInputDevice : rightInputDevice;

        if (action != null && inputDevice != null)
        {
            OpenXRInput.SendHapticImpulse(action, amplitude, frequency, duration, inputDevice);
            return;
        }
    }
}