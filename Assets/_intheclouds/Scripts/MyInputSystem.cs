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

    public bool IsGripActive(HandSide handSide)
    {
        float grip = 0f;
        if (handSide == HandSide.Left)
        {
            grip = MyInputActions.XRILeftInteraction.Grip.ReadValue<float>();
        }
        else
        {
            grip = MyInputActions.XRIRightInteraction.Grip.ReadValue<float>();
        }

        return grip >= 0.75f;
    }
    
    public bool WasGripActivated(HandSide handSide)
    {
        bool activated;
        if (handSide == HandSide.Left)
        {
            activated = MyInputActions.XRILeftInteraction.Grip.WasPressedThisFrame();
        }
        else
        {
            activated = MyInputActions.XRIRightInteraction.Grip.WasPressedThisFrame();
        }

        return activated;
    }
    
    public bool WasGripDeactivated(HandSide handSide)
    {
        bool deactivated;
        if (handSide == HandSide.Left)
        {
            deactivated = MyInputActions.XRILeftInteraction.Grip.WasReleasedThisFrame();
        }
        else
        {
            deactivated = MyInputActions.XRIRightInteraction.Grip.WasReleasedThisFrame();
        }

        return deactivated;
    }

    public bool IsTriggerActive(HandSide handSide)
    {
        float trigger = 0f;
        if (handSide == HandSide.Left)
        {
            trigger = MyInputActions.XRILeftInteraction.Trigger.ReadValue<float>();
        }
        else
        {
            trigger = MyInputActions.XRIRightInteraction.Trigger.ReadValue<float>();
        }

        return trigger >= 0.75f;
    }
    
    public bool WasTriggerActivated(HandSide handSide)
    {
        bool activated;
        if (handSide == HandSide.Left)
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

    public void Vibrate(HandSide handSide, float amplitude, float duration = 1, float frequency = 1)
    {
        var action = handSide == HandSide.Left ? MyInputActions.XRILeft.HapticDevice : MyInputActions.XRIRight.HapticDevice;
        var inputDevice = handSide == HandSide.Left ? leftInputDevice : rightInputDevice;

        if (action != null && inputDevice != null)
        {
            OpenXRInput.SendHapticImpulse(action, amplitude, frequency, duration, inputDevice);
            return;
        }
    }
}