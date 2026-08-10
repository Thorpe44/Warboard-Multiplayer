using UnityEngine;

// WARBOARD_V54_PLACEMENT_GHOST_VISUAL
public partial class ModelToken : MonoBehaviour
{
    public GameObject CreatePlacementGhost54()
    {
        GameObject wrapper =
            new GameObject(
                "Placement Ghost - " +
                (RoleName ?? "Model")
            );

        wrapper.transform.position =
            transform.position;

        wrapper.transform.rotation =
            transform.rotation;

        wrapper.transform.localScale =
            Vector3.one;

        if (visualRoot != null)
        {
            GameObject visual =
                Object.Instantiate(
                    visualRoot,
                    wrapper.transform
                );

            visual.name = "Ghost Visual";
            visual.transform.localPosition =
                visualRoot.transform.localPosition;
            visual.transform.localRotation =
                visualRoot.transform.localRotation;
            visual.transform.localScale =
                visualRoot.transform.localScale;
        }
        else
        {
            GameObject proxy =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cylinder
                );

            proxy.name = "Ghost Base Proxy";

            proxy.transform.SetParent(
                wrapper.transform,
                false
            );

            proxy.transform.localPosition =
                new Vector3(
                    0f,
                    -0.54f,
                    0f
                );

            proxy.transform.localScale =
                new Vector3(
                    BaseRadiusInches * 2f,
                    0.05f,
                    BaseRadiusInches * 2f
                );
        }

        foreach (Transform child
            in wrapper
                .GetComponentsInChildren<
                    Transform
                >(true))
        {
            if (child != null)
                child.gameObject.layer = 2;
        }

        foreach (Collider collider
            in wrapper
                .GetComponentsInChildren<
                    Collider
                >(true))
        {
            if (collider == null)
                continue;

            collider.enabled = false;
            Object.Destroy(collider);
        }

        foreach (Rigidbody body
            in wrapper
                .GetComponentsInChildren<
                    Rigidbody
                >(true))
        {
            if (body == null)
                continue;

            body.detectCollisions = false;
            body.isKinematic = true;
            Object.Destroy(body);
        }

        foreach (Behaviour behaviour
            in wrapper
                .GetComponentsInChildren<
                    Behaviour
                >(true))
        {
            if (behaviour != null)
                behaviour.enabled = false;
        }

        return wrapper;
    }
}
