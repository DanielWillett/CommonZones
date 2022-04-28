using CommonZones.Models;
using Rocket.API;
using System;
using System.Xml;
using System.Xml.Serialization;


namespace CommonZones;

public class CommonZonesConfig : IRocketPluginConfiguration
{
    public string StorageType = null!;
    /// <summary>Leave at 0 for every tick.</summary>
    public float CheckTimeSeconds;
    public Guid CurrentZoneEffectID;
    public Guid ZoneEditHUDID;
    [XmlElement("MySql")]
    public MySqlData MySQL;
    public void LoadDefaults()
    {
        StorageType             = EZoneStorageType.JSON.ToString();
        CheckTimeSeconds        = 0.25f;
        CurrentZoneEffectID     = new Guid("d747f8ae47d1438ba3ba5c2cc734ee04");
        ZoneEditHUDID           = new Guid("503fed1019db4c7e9c365bf6e108b43f");
        MySQL = new MySqlData("127.0.0.1", "unturned", "password", "root", 3306, "utf8");
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
