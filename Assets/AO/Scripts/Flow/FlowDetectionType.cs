namespace AerodynamicObjects
{
    /// <summary>
    /// Used to specify how an object should detect fluid zones. Collider is adaptable and will collect any fluid zones that the object comes into contact with.
    /// List uses a pre-assigned collection of fluid zones which can be updated programmatically.
    /// </summary>
    [System.Serializable]
    public enum FlowDetectionType
    {
        Collider,
        ColliderIgnoreTransformHierarchy,
        List
    }
}