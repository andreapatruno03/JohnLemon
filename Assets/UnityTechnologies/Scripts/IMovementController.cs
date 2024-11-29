using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMovementController
{
    void UpdateVisionState(bool canSeeTarget, Transform target);
}

