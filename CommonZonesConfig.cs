using Rocket.API;
using System.Xml;
using System.Xml.Serialization;


namespace CommonZones;

public class CommonZonesConfig : IRocketPluginConfiguration
{
    public string StorageType = null!;
    /// <summary>Leave at 0 for every tick.</summary>
    public float CheckTimeSeconds;
    public void LoadDefaults()
    {
        StorageType = EZoneStorageType.JSON.ToString();
        CheckTimeSeconds = 0.25f;
    }
}
public enum EZoneStorageType : byte
{
    [XmlEnum(Name = "MySQL")]
    MYSQL,
    [XmlEnum(Name = "JSON")]
    JSON,
    [XmlEnum(Name = "XML")]
    XML
}
