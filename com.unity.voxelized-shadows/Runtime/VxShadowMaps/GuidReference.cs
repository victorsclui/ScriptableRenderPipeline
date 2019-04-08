using UnityEngine;

[System.Serializable]
public class GuidReference : ISerializationCallbackReceiver
{
    [SerializeField]
    private byte[] _serializedGuid;
    private System.Guid Guid = System.Guid.Empty;

    public GuidReference()
    {
    }

    public void OnBeforeSerialize()
    {
        if (Guid != System.Guid.Empty)
        {
            _serializedGuid = Guid.ToByteArray();
        }
    }

    public void OnAfterDeserialize()
    {
        if (_serializedGuid != null && _serializedGuid.Length == 16)
        {
            Guid = new System.Guid(_serializedGuid);
        }
    }

    public System.Guid GetGuid()
    {
        if (Guid == System.Guid.Empty)
            Guid = System.Guid.NewGuid();

        return Guid;
    }

    public byte[] GetGuidByteArray()
    {
        return GetGuid().ToByteArray();
    }
}
