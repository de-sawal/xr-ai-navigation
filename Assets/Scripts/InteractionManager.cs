using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using UnityEngine.Events;

public class InteractionManager : MonoBehaviour
{
    [SerializeField] private LineRenderer pointerLine;
    [SerializeField] private float maxPointerDistance = 10f;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Material highlightMaterial;

    private XRNode controllerNode = XRNode.RightHand;
    private GameObject currentHighlightedObject;
    private Material[] originalMaterials;
    private bool isSelectionEnabled = true;

    public UnityEvent<GameObject> OnObjectSelected;

    private void Awake()
    {
        if (OnObjectSelected == null)
            OnObjectSelected = new UnityEvent<GameObject>();
    }

    private void Update()
    {
        if (!isSelectionEnabled) return;

        UpdatePointer();
        CheckForSelection();
    }

    private void UpdatePointer()
    {
        List<XRNodeState> nodeStates = new List<XRNodeState>();
        InputTracking.GetNodeStates(nodeStates);

        foreach (XRNodeState state in nodeStates)
        {
            if (state.nodeType == controllerNode)
            {
                state.TryGetPosition(out Vector3 position);
                state.TryGetRotation(out Quaternion rotation);

                Ray pointerRay = new Ray(position, rotation * Vector3.forward);
                RaycastHit hit;

                Vector3 endPoint = position + (rotation * Vector3.forward * maxPointerDistance);

                if (Physics.Raycast(pointerRay, out hit, maxPointerDistance, interactableLayer))
                {
                    endPoint = hit.point;
                    HandleObjectHighlight(hit.collider.gameObject);
                }
                else
                {
                    ClearHighlight();
                }

                // Update line renderer
                pointerLine.SetPosition(0, position);
                pointerLine.SetPosition(1, endPoint);
            }
        }
    }

    private void CheckForSelection()
    {
        if (currentHighlightedObject == null) return;

        // Check for trigger press
        InputDevice device = InputDevices.GetDeviceAtXRNode(controllerNode);
        bool triggerPressed;

        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out triggerPressed) && triggerPressed)
        {
            OnObjectSelected?.Invoke(currentHighlightedObject);
            ClearHighlight();
        }
    }

    private void HandleObjectHighlight(GameObject obj)
    {
        if (obj == currentHighlightedObject) return;

        ClearHighlight();

        currentHighlightedObject = obj;
        MeshRenderer renderer = obj.GetComponent<MeshRenderer>();

        if (renderer != null)
        {
            originalMaterials = renderer.materials;
            Material[] highlightMaterials = new Material[renderer.materials.Length];
            for (int i = 0; i < highlightMaterials.Length; i++)
            {
                highlightMaterials[i] = highlightMaterial;
            }
            renderer.materials = highlightMaterials;
        }
    }

    private void ClearHighlight()
    {
        if (currentHighlightedObject != null)
        {
            MeshRenderer renderer = currentHighlightedObject.GetComponent<MeshRenderer>();
            if (renderer != null && originalMaterials != null)
            {
                renderer.materials = originalMaterials;
            }
            currentHighlightedObject = null;
        }
    }

    public void SetSelectionEnabled(bool enabled)
    {
        isSelectionEnabled = enabled;
        pointerLine.enabled = enabled;
        if (!enabled)
        {
            ClearHighlight();
        }
    }
}



