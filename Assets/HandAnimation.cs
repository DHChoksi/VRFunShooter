using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class HandAnimation : MonoBehaviour
{
    [SerializeField]
    private InputActionReference m_Grip;

    [SerializeField]
    private InputActionReference m_Trigger;

/*    [SerializeField]
    private ActionBasedController m_Controller;

*/    [SerializeField]
    private Animator m_Animator = null;
    
    // Add a public reference to differentiate between left and right controllers
 //   public bool isLeftHand;

    /*  void Start()
      {
          // Find the XR Origin in the scene
          XRBaseController[] controllers = FindObjectsOfType<ActionBasedController>();

          foreach (var controller in controllers)
          {
              // Assuming left hand uses left controller and vice versa
              if (isLeftHand && controller.name.ToLower().Contains("left"))
              {
                  m_Controller = (ActionBasedController)controller;
              }
              else if (!isLeftHand && controller.name.ToLower().Contains("right"))
              {
                  m_Controller = (ActionBasedController)controller;
              }
          }

          if (m_Controller == null)
          {
              Debug.LogError("ActionBasedController not found for " + (isLeftHand ? "left" : "right") + " hand.");
          }
      }

      void Update()
      {
          if (m_Controller != null)
          {
              float gripValue = m_Controller.selectAction.action.ReadValue<float>();
              float triggerValue = m_Controller.selectAction.action.ReadValue<float>();

              m_Animator.SetFloat("Grip", gripValue);
              m_Animator.SetFloat("Trigger", triggerValue);

          }
      } */

    void Update()
    {
        
         float gripValue = m_Grip.action.ReadValue<float>();
         float triggerValue = m_Trigger.action.ReadValue<float>();

         m_Animator.SetFloat("Grip", gripValue);
         m_Animator.SetFloat("Trigger", triggerValue);

    }
}
