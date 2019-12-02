using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class DataHelper {
    // Start is called before the first frame update

    public static IO.Swagger.Model.Position Vector3ToPosition(Vector3 vector) {
        return new IO.Swagger.Model.Position((decimal) vector.x, (decimal) vector.y, (decimal) vector.z);
    }

    public static Vector3 PositionToVector3(IO.Swagger.Model.Position position) => new Vector3((float) position.X, (float) position.Y, (float) position.Z);

    public static IO.Swagger.Model.Orientation QuaternionToOrientation(Quaternion quaternion) {
        return new IO.Swagger.Model.Orientation((decimal) quaternion.w, (decimal) quaternion.x, (decimal) quaternion.y, (decimal) quaternion.z);
    }

    public static Quaternion OrientationToQuaternion(IO.Swagger.Model.Orientation orientation) => new Quaternion((float) orientation.X, (float) orientation.Y, (float) orientation.Z, (float) orientation.W);

    public static IO.Swagger.Model.Pose CreatePose(Vector3 position, Quaternion orientation) {
        return new IO.Swagger.Model.Pose(QuaternionToOrientation(orientation), Vector3ToPosition(position));
    }

    public static void GetPose(IO.Swagger.Model.Pose pose, out Vector3 position, out Quaternion orientation) {
        position = PositionToVector3(pose.Position);
        orientation = OrientationToQuaternion(pose.Orientation);
    }

    public static IO.Swagger.Model.ProjectObject SceneObjectToProjectObject(IO.Swagger.Model.SceneObject sceneObject) {
        IO.Swagger.Model.ProjectObject projectObject = new IO.Swagger.Model.ProjectObject(new List<IO.Swagger.Model.ProjectActionPoint>(), sceneObject.Id);
        return projectObject;
    }

    public static IO.Swagger.Model.ProjectActionPoint ActionPointToProjectActionPoint(IO.Swagger.Model.ActionPoint actionPoint) {
        return new IO.Swagger.Model.ProjectActionPoint(id: actionPoint.Id, joints: actionPoint.Joints, orientations: actionPoint.Orientations,
            position: actionPoint.Position, actions: new List<IO.Swagger.Model.Action>());
    }

    public static IO.Swagger.Model.ActionPoint ProjectActionPointToActionPoint(IO.Swagger.Model.ProjectActionPoint projectActionPoint) {
        return new IO.Swagger.Model.ActionPoint(id: projectActionPoint.Id, joints: projectActionPoint.Joints,
            orientations: projectActionPoint.Orientations, position: projectActionPoint.Position);
    }
}
