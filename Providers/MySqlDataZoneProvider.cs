using CommonZones.API;
using CommonZones.Models;
using CommonZones.Zones;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace CommonZones.Providers;
// decided to go synchronous for this since it will simplify implementation with the other providers and since it's not going to be called often.
internal class MySqlDataZoneProvider : IZoneProvider
{
    public List<Zone> Zones => _zones;
    private readonly List<Zone> _zones;
    private readonly FileInfo _file;
    public MySqlDataZoneProvider(FileInfo file)
    {
        this._file = file;
        this._zones = new List<Zone>();
        _login = CommonZones.I.Configuration.Instance.MySQL;
        SQL = new MySqlConnection(_login.GetConnectionString());
        DebugLogging = true;
        if (!Open())
        {
            L.LogError("Failed to connect to a MySql server for the reason above. Unloading until this is resolved.");
            throw new ZoneAPIException();
        }
    }

    public void SaveZone(Zone zone)
    {
        int i = _zones.IndexOf(zone ?? throw new ArgumentNullException(nameof(zone)));
        if (i == -1)
        {
            for (int j = 0; j < _zones.Count; ++j)
            {
                if (_zones[j].Name.Equals(zone.Name, StringComparison.Ordinal))
                {
                    i = j;
                    break;
                }
            }
        }

        if (i == -1)
            throw new ZoneAPIException("Zone not found in list.");
        SaveZone(i);
    }
    public void SaveZone(int index)
    {
        if (!hasVerified)
        {
            if (!VerifyTableIntegrety()) return;
            hasVerified = true;
        }
        
        if (index < 0 || index >= _zones.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index), );
        }

        Zone zone = _zones[index];

        bool hasPk = zone.OptPrimaryKey > -1;

        int pk = -1;

        string query;
        if (!hasPk)
        {
            Query("SELECT `pk` FROM `cz_zone_data` WHERE `Name` = @0 LIMIT 1;", new object[1] { zone.Name }, R => pk = R.GetInt32(0));
        }
        if (pk == -1)
        {
            query =
                "INSERT INTO `cz_zone_data` (`Name`, `ShortName`, `X`, `Z`, `MinHeight`, `MaxHeight`, `Type`, `UsesMapCoords`) " +
                "VALUES (@0, @1, @2, @3, @4, @5, @6, @7) ON DUPLICATE KEY UPDATE " +
                "`Name` = @0, `ShortName` = @1, `X` = @2, `Z` = @3, `MinHeight` = @4, `MaxHeight` = @5, `Type` = @6, `UsesMapCoords` = @7, `pk` = LAST_INSERT_ID(`pk`); " +
                "SET @zonePk := (SELECT LAST_INSERT_ID() AS `pk`); " +
                "DELETE FROM `cz_zone_circles` WHERE `ZonePk` = @zonePk; " +
                "DELETE FROM `cz_zone_rectangles` WHERE `ZonePk` = @zonePk; " +
                "DELETE FROM `cz_zone_polygon_points` WHERE `ZonePk` = @zonePk; " +
                "DELETE FROM `cz_zone_tags` WHERE `ZonePk` = @zonePk; " +
                "SELECT @zonePk;";
        }
        else
        {
            query =
                "INSERT INTO `cz_zone_data` (`pk`, `Name`, `ShortName`, `X`, `Z`, `MinHeight`, `MaxHeight`, `Type`, `UsesMapCoords`) " +
                "VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8) ON DUPLICATE KEY UPDATE " +
                "`Name` = @1, `ShortName` = @2, `X` = @3, `Z` = @4, `MinHeight` = @5, `MaxHeight` = @6, `Type` = @7, `UsesMapCoords` = @8, `pk` = LAST_INSERT_ID(`pk`); " +
                "SET @zonePk := (SELECT LAST_INSERT_ID() AS `pk`); " +
                "DELETE FROM `cz_zone_circles` WHERE `ZonePk` = @zonePk; " +
                "DELETE FROM `cz_zone_rectangles` WHERE `ZonePk` = @zonePk; " +
                "DELETE FROM `cz_zone_polygon_points` WHERE `ZonePk` = @zonePk; " +
                "DELETE FROM `cz_zone_tags` WHERE `ZonePk` = @zonePk; " +
                "SELECT @zonePk;";
        }

        Query(query, pk == -1
            ? new object[8]
            {

            }
            : new object[9]
            {

            });
    }
    private void AddPluginZones()
    {
        if (API.Zones.PluginZonesHasSubscriptions)
        {
            ZoneBuilderCollection collection = new ZoneBuilderCollection(8);
            API.Zones.OnRegisterPluginsNeeded(collection);
            if (collection.Count > 0)
            {
                for (int i = 0; i < collection.Count; ++i)
                    _zones.Add(collection[i].GetZone());
            }
        }
    }

    public bool VerifyTableIntegrety()
    {
        bool bad = false;
        try
        {
            List<string> tables = new List<string>(5);
            Query("SHOW TABLES LIKE \"cz_zone_%\";", null, R =>
            {
                tables.Add(R.GetString(0));
            });
            for (int i = 0; i < MySqlConstants.TABLES.Length; ++i)
            {
                string tblName = MySqlConstants.TABLES[i];
                bool exists = false;
                for (int j = 0; j < tables.Count; ++j)
                {
                    if (tblName.Equals(tables[j], StringComparison.Ordinal))
                    {
                        exists = true;
                        break;
                    }
                }
                try
                {
                    if (exists)
                    {
                        List<MySqlColumnData> columns = new List<MySqlColumnData>(9);
                        List<int>? toDelete = null;
                        Query("DESCRIBE `" + tblName + "`;", null, R =>
                        {
                            columns.Add(new MySqlColumnData(R.GetString(0), R.GetString(1),
                                R.GetString(2).Equals("YES", StringComparison.OrdinalIgnoreCase), R.GetString(3),
                                R.GetStringOrNull(4), R.GetStringOrNull(5)));
                        });
                        switch (i)
                        {
                            // 0 = not found, 1 = not accurate, 2 = all good
                            case 0: // data
                                byte pk = 0, name = 0, sname = 0, x = 0, z = 0, minheight = 0, maxheight = 0, type = 0, usesMapCoords = 0;
                                for (int j = 0; j < columns.Count; j++)
                                {
                                    MySqlColumnData data = columns[j];
                                    string field = data.Field;
                                    if (field.Equals("pk", StringComparison.Ordinal))
                                    {
                                        pk = (data.Type.Equals("int unsigned", StringComparison.OrdinalIgnoreCase) &&
                                             !data.Null && data.Key.Equals("PRI", StringComparison.OrdinalIgnoreCase) &&
                                             data.Extra != null && data.Extra.Equals("auto_increment",
                                                 StringComparison.OrdinalIgnoreCase))
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else if (field.Equals("Name", StringComparison.Ordinal))
                                    {
                                        name = (data.Type.Equals("varchar(128)", StringComparison.OrdinalIgnoreCase) &&
                                                !data.Null)
                                            ? (byte)2
                                            : (byte)1;
                                        if (!data.Key.Equals("UNI", StringComparison.OrdinalIgnoreCase))
                                            name += 2;
                                    }
                                    else if (field.Equals("ShortName", StringComparison.Ordinal))
                                    {
                                        sname = (data.Type.Equals("varchar(64)", StringComparison.OrdinalIgnoreCase) && data.Null)
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else if (field.Equals("X", StringComparison.Ordinal))
                                    {
                                        x = (data.Type.Equals("float", StringComparison.OrdinalIgnoreCase) && !data.Null)
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else if (field.Equals("Z", StringComparison.Ordinal))
                                    {
                                        z = (data.Type.Equals("float", StringComparison.OrdinalIgnoreCase) && !data.Null)
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else if (field.Equals("MinHeight", StringComparison.Ordinal))
                                    {
                                        minheight = (data.Type.Equals("float", StringComparison.OrdinalIgnoreCase) && data.Null)
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else if (field.Equals("MaxHeight", StringComparison.Ordinal))
                                    {
                                        maxheight = (data.Type.Equals("float", StringComparison.OrdinalIgnoreCase) && data.Null)
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else if (field.Equals("Type", StringComparison.Ordinal))
                                    {
                                        type = (data.Type.Equals("tinyint unsigned", StringComparison.OrdinalIgnoreCase) && !data.Null)
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else if (field.Equals("UsesMapCoords", StringComparison.Ordinal))
                                    {
                                        usesMapCoords = (data.Type.Equals("bit(1)", StringComparison.OrdinalIgnoreCase) && !data.Null)
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else
                                    {
                                        if (toDelete == null)
                                            toDelete = new List<int>(1) { j };
                                        else
                                            toDelete.Insert(0, j);
                                    }
                                }

                                if (pk != 2 || name != 2 || sname != 2 || x != 2 || z != 2 || minheight != 2 ||
                                    maxheight != 2 || type != 2 || usesMapCoords != 2 || (toDelete != null && toDelete.Count > 0))
                                {
                                    StringBuilder query = new StringBuilder("ALTER TABLE `cz_zone_data` ", 128);
                                    // columns need changed
                                    if (pk == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`pk` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `pk` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT, ");
                                    }
                                    else if (pk == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`pk` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `pk` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT, ");
                                    }

                                    if (name == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`Name` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `Name` VARCHAR(128) NOT NULL COLLATE 'utf8_unicode_ci' AFTER `pk`, ADD UNIQUE(`Name`), ");
                                    }
                                    else if (name == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`Name` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `Name` VARCHAR(128) NOT NULL COLLATE 'utf8_unicode_ci' AFTER `pk`, ");
                                    }
                                    else if (name == 3)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`Name` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `Name` VARCHAR(128) NOT NULL COLLATE 'utf8_unicode_ci' AFTER `pk`, ADD UNIQUE (`Name`), ");
                                    }
                                    else if (name == 4)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`Name` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("ADD UNIQUE (`Name`), ");
                                    }

                                    if (sname == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`ShortName` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `ShortName` VARCHAR(64) NULL DEFAULT NULL COLLATE 'utf8_unicode_ci' AFTER `Name`, ");
                                    }
                                    else if (sname == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`ShortName` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `ShortName` VARCHAR(64) NULL DEFAULT NULL COLLATE 'utf8_unicode_ci' AFTER `Name`, ");
                                    }

                                    if (x == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`X` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `X` FLOAT NOT NULL AFTER `ShortName`, ");
                                    }
                                    else if (x == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`X` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `X` FLOAT NOT NULL AFTER `ShortName`, ");
                                    }

                                    if (z == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`Z` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `Z` FLOAT NOT NULL AFTER `X`, ");
                                    }
                                    else if (z == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`Z` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `Z` FLOAT NOT NULL AFTER `X`, ");
                                    }

                                    if (minheight == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`MinHeight` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `MinHeight` FLOAT NULL DEFAULT NULL AFTER `Z`, ");
                                    }
                                    else if (minheight == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`MinHeight` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `MinHeight` FLOAT NULL DEFAULT NULL AFTER `Z`, ");
                                    }

                                    if (maxheight == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`MaxHeight` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `MaxHeight` FLOAT NULL DEFAULT NULL AFTER `MinHeight`, ");
                                    }
                                    else if (maxheight == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`MaxHeight` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `MaxHeight` FLOAT NULL DEFAULT NULL AFTER `MinHeight`, ");
                                    }

                                    if (type == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`Type` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `Type` TINYINT(3) UNSIGNED NOT NULL AFTER `MaxHeight`, ");
                                    }
                                    else if (type == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`Type` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `Type` TINYINT(3) UNSIGNED NOT NULL AFTER `MaxHeight`, ");
                                    }

                                    if (usesMapCoords == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`UsesMapCoords` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `UsesMapCoords` BIT(1) NOT NULL DEFAULT 0 AFTER `Type`, ");
                                    }
                                    else if (usesMapCoords == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_data`.`UsesMapCoords` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `UsesMapCoords` BIT(1) NOT NULL DEFAULT 0 AFTER `Type`, ");
                                    }

                                    if (toDelete != null)
                                    {
                                        for (int j = 0; j < toDelete.Count; ++j)
                                        {
                                            string c = columns[toDelete[j]].Field;
                                            L.Log("[MYSQL VALIDATOR] Extra column `cz_zone_data`.`" + c + "` found, deleting it.", ConsoleColor.DarkYellow);
                                            query.Append("DROP COLUMN `" + c + "`, ");
                                        }
                                    }

                                    string q2 = query.ToString();
                                    if (q2.Length > 1 && q2[q2.Length - 2] == ',')
                                    {
                                        q2 = q2.Substring(0, q2.Length - 2) + ";";
                                    }

                                    NonQuery(q2, null);
                                }
                                L.Log("[MYSQL VALIDATOR] Table `cz_zone_data` validated.", ConsoleColor.DarkYellow);
                                break;
                            case 1: // tags
                                pk = 0;
                                byte zonepk = 0, tag = 0;
                                for (int j = 0; j < columns.Count; j++)
                                {
                                    MySqlColumnData data = columns[j];
                                    string field = data.Field;
                                    if (field.Equals("pk", StringComparison.Ordinal))
                                    {
                                        pk = (data.Type.Equals("int unsigned", StringComparison.OrdinalIgnoreCase) &&
                                             !data.Null && data.Key.Equals("PRI", StringComparison.OrdinalIgnoreCase) &&
                                             data.Extra != null && data.Extra.Equals("auto_increment",
                                                 StringComparison.OrdinalIgnoreCase))
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else if (field.Equals("ZonePk", StringComparison.Ordinal))
                                    {
                                        zonepk = (data.Type.Equals("int unsigned", StringComparison.OrdinalIgnoreCase) &&
                                              !data.Null && data.Key.Equals("MUL",
                                                  StringComparison.OrdinalIgnoreCase))
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else if (field.Equals("Tag", StringComparison.Ordinal))
                                    {
                                        tag = (data.Type.Equals("varchar(256)", StringComparison.OrdinalIgnoreCase) && !data.Null)
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else
                                    {
                                        if (toDelete == null)
                                            toDelete = new List<int>(1) { j };
                                        else
                                            toDelete.Insert(0, j);
                                    }
                                }

                                if (pk != 2 || zonepk != 2 || tag != 2 || (toDelete != null && toDelete.Count > 0))
                                {
                                    StringBuilder query = new StringBuilder("ALTER TABLE `cz_zone_tags` ", 64);

                                    if (pk == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_tags`.`pk` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `pk` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT, ");
                                    }
                                    else if (pk == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_tags`.`pk` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `pk` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT, ");
                                    }

                                    if (zonepk == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_tags`.`ZonePk` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `ZonePk` INT(10) UNSIGNED NOT NULL AFTER `pk`, ");
                                    }
                                    else if (zonepk == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_tags`.`ZonePk` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `ZonePk` INT(10) UNSIGNED NOT NULL AFTER `pk`, ");
                                    }

                                    if (tag == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_tags`.`Tag` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `Tag` VARCHAR(256) NOT NULL COLLATE 'utf8_unicode_ci' AFTER `ZonePk`, ");
                                    }
                                    else if (tag == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_tags`.`Tag` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `Tag` VARCHAR(256) NOT NULL COLLATE 'utf8_unicode_ci' AFTER `ZonePk`, ");
                                    }

                                    if (toDelete != null)
                                    {
                                        for (int j = 0; j < toDelete.Count; ++j)
                                        {
                                            string c = columns[toDelete[j]].Field;
                                            L.Log("[MYSQL VALIDATOR] Extra column `cz_zone_tags`.`" + c + "` found, deleting it.", ConsoleColor.DarkYellow);
                                            query.Append("DROP COLUMN `" + c + "`, ");
                                        }
                                    }

                                    string q2 = query.ToString();
                                    if (q2.Length > 1 && q2[q2.Length - 2] == ',')
                                    {
                                        q2 = q2.Substring(0, q2.Length - 2) + ";";
                                    }

                                    NonQuery(q2, null);
                                }
                                L.Log("[MYSQL VALIDATOR] Table `cz_zone_tags` validated.", ConsoleColor.DarkYellow);
                                break;
                            case 2: // circles
                                zonepk = 0;
                                byte radius = 0;
                                for (int j = 0; j < columns.Count; j++)
                                {
                                    MySqlColumnData data = columns[j];
                                    string field = data.Field;
                                    if (field.Equals("ZonePk", StringComparison.Ordinal))
                                    {
                                        zonepk = (data.Type.Equals("int unsigned", StringComparison.OrdinalIgnoreCase) &&
                                             !data.Null && data.Key.Equals("PRI", StringComparison.OrdinalIgnoreCase) &&
                                             data.Extra != null && data.Extra.Equals("auto_increment",
                                                 StringComparison.OrdinalIgnoreCase))
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else if (field.Equals("Radius", StringComparison.Ordinal))
                                    {
                                        radius = (data.Type.Equals("float", StringComparison.OrdinalIgnoreCase) && !data.Null)
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else
                                    {
                                        if (toDelete == null)
                                            toDelete = new List<int>(1) { j };
                                        else
                                            toDelete.Insert(0, j);
                                    }
                                }

                                if (zonepk != 2 || radius != 2 || (toDelete != null && toDelete.Count > 0))
                                {
                                    StringBuilder query = new StringBuilder("ALTER TABLE `cz_zone_circles` ", 64);

                                    if (zonepk == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_circles`.`ZonePk` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `ZonePk` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT, ");
                                    }
                                    else if (zonepk == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_circles`.`ZonePk` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `ZonePk` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT, ");
                                    }

                                    if (radius == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_circles`.`Radius` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `Radius` FLOAT NOT NULL AFTER `ZonePk`, ");
                                    }
                                    else if (radius == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_circles`.`Radius` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `Radius` FLOAT NOT NULL AFTER `ZonePk`, ");
                                    }

                                    if (toDelete != null)
                                    {
                                        for (int j = 0; j < toDelete.Count; ++j)
                                        {
                                            string c = columns[toDelete[j]].Field;
                                            L.Log("[MYSQL VALIDATOR] Extra column `cz_zone_circles`.`" + c + "` found, deleting it.", ConsoleColor.DarkYellow);
                                            query.Append("DROP COLUMN `" + c + "`, ");
                                        }
                                    }

                                    string q2 = query.ToString();
                                    if (q2.Length > 1 && q2[q2.Length - 2] == ',')
                                    {
                                        q2 = q2.Substring(0, q2.Length - 2) + ";";
                                    }

                                    NonQuery(q2, null);
                                }
                                L.Log("[MYSQL VALIDATOR] Table `cz_zone_circles` validated.", ConsoleColor.DarkYellow);
                                break;
                            case 3: // rectangles
                                zonepk = 0;
                                byte sizex = 0, sizez = 0;
                                for (int j = 0; j < columns.Count; j++)
                                {
                                    MySqlColumnData data = columns[j];
                                    string field = data.Field;
                                    if (field.Equals("ZonePk", StringComparison.Ordinal))
                                    {
                                        zonepk = (data.Type.Equals("int unsigned", StringComparison.OrdinalIgnoreCase) &&
                                                  !data.Null && data.Key.Equals("PRI", StringComparison.OrdinalIgnoreCase) &&
                                                  data.Extra != null && data.Extra.Equals("auto_increment",
                                                      StringComparison.OrdinalIgnoreCase))
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else if (field.Equals("SizeX", StringComparison.Ordinal))
                                    {
                                        sizex = (data.Type.Equals("float", StringComparison.OrdinalIgnoreCase) && !data.Null)
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else if (field.Equals("SizeZ", StringComparison.Ordinal))
                                    {
                                        sizez = (data.Type.Equals("float", StringComparison.OrdinalIgnoreCase) && !data.Null)
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else
                                    {
                                        if (toDelete == null)
                                            toDelete = new List<int>(1) { j };
                                        else
                                            toDelete.Insert(0, j);
                                    }
                                }

                                if (zonepk != 2 || sizex != 2 || sizez != 2 || (toDelete != null && toDelete.Count > 0))
                                {
                                    StringBuilder query = new StringBuilder("ALTER TABLE `cz_zone_rectangles` ", 64);

                                    if (zonepk == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_rectangles`.`ZonePk` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `ZonePk` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT, ");
                                    }
                                    else if (zonepk == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_rectangles`.`ZonePk` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `ZonePk` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT, ");
                                    }

                                    if (sizex == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_rectangles`.`SizeX` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `SizeX` FLOAT NOT NULL AFTER `ZonePk`, ");
                                    }
                                    else if (sizex == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_rectangles`.`SizeX` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `SizeX` FLOAT NOT NULL AFTER `ZonePk`, ");
                                    }

                                    if (sizez == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_rectangles`.`SizeZ` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `SizeZ` FLOAT NOT NULL AFTER `SizeX`, ");
                                    }
                                    else if (sizez == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_rectangles`.`SizeZ` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `SizeZ` FLOAT NOT NULL AFTER `SizeX`, ");
                                    }

                                    if (toDelete != null)
                                    {
                                        for (int j = 0; j < toDelete.Count; ++j)
                                        {
                                            string c = columns[toDelete[j]].Field;
                                            L.Log("[MYSQL VALIDATOR] Extra column `cz_zone_rectangles`.`" + c + "` found, deleting it.", ConsoleColor.DarkYellow);
                                            query.Append("DROP COLUMN `" + c + "`, ");
                                        }
                                    }

                                    string q2 = query.ToString();
                                    if (q2.Length > 1 && q2[q2.Length - 2] == ',')
                                    {
                                        q2 = q2.Substring(0, q2.Length - 2) + ";";
                                    }

                                    NonQuery(q2, null);
                                }
                                L.Log("[MYSQL VALIDATOR] Table `cz_zone_rectangles` validated.", ConsoleColor.DarkYellow);
                                break;
                            case 4: // points
                                pk = 0;
                                zonepk = 0;
                                x = 0;
                                z = 0;
                                for (int j = 0; j < columns.Count; j++)
                                {
                                    MySqlColumnData data = columns[j];
                                    string field = data.Field;
                                    if (field.Equals("pk", StringComparison.Ordinal))
                                    {
                                        pk = (data.Type.Equals("int unsigned", StringComparison.OrdinalIgnoreCase) &&
                                              !data.Null && data.Key.Equals("PRI", StringComparison.OrdinalIgnoreCase) &&
                                              data.Extra != null && data.Extra.Equals("auto_increment",
                                                  StringComparison.OrdinalIgnoreCase))
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else if (field.Equals("ZonePk", StringComparison.Ordinal))
                                    {
                                        zonepk = (data.Type.Equals("int unsigned", StringComparison.OrdinalIgnoreCase) &&
                                                  !data.Null && data.Key.Equals("MUL",
                                                      StringComparison.OrdinalIgnoreCase))
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else if (field.Equals("X", StringComparison.Ordinal))
                                    {
                                        x = (data.Type.Equals("float", StringComparison.OrdinalIgnoreCase) && !data.Null)
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else if (field.Equals("Z", StringComparison.Ordinal))
                                    {
                                        z = (data.Type.Equals("float", StringComparison.OrdinalIgnoreCase) && !data.Null)
                                            ? (byte)2
                                            : (byte)1;
                                    }
                                    else
                                    {
                                        if (toDelete == null)
                                            toDelete = new List<int>(1) { j };
                                        else
                                            toDelete.Insert(0, j);
                                    }
                                }

                                if (pk != 2 || zonepk != 2 || x != 2 || z != 2 || (toDelete != null && toDelete.Count > 0))
                                {
                                    StringBuilder query = new StringBuilder("ALTER TABLE `cz_zone_polygon_points` ", 96);

                                    if (pk == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_polygon_points`.`pk` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `pk` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT, ");
                                    }
                                    else if (pk == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_polygon_points`.`pk` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `pk` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT, ");
                                    }

                                    if (zonepk == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_polygon_points`.`ZonePk` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `ZonePk` INT(10) UNSIGNED NOT NULL AFTER `pk`, ");
                                    }
                                    else if (zonepk == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_polygon_points`.`ZonePk` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `ZonePk` INT(10) UNSIGNED NOT NULL AFTER `pk`, ");
                                    }

                                    if (x == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_polygon_points`.`X` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `X` FLOAT NOT NULL AFTER `ZonePk`, ");
                                    }
                                    else if (x == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_polygon_points`.`X` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `X` FLOAT NOT NULL AFTER `ZonePk`, ");
                                    }

                                    if (z == 0)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_polygon_points`.`Z` not found, adding it to the table.", ConsoleColor.DarkYellow);
                                        query.Append("ADD `Z` FLOAT NOT NULL AFTER `X`, ");
                                    }
                                    else if (z == 1)
                                    {
                                        L.Log("[MYSQL VALIDATOR] Column `cz_zone_polygon_points`.`Z` doesn't have the right definition, changing it.", ConsoleColor.DarkYellow);
                                        query.Append("MODIFY `Z` FLOAT NOT NULL AFTER `X`, ");
                                    }

                                    if (toDelete != null)
                                    {
                                        for (int j = 0; j < toDelete.Count; ++j)
                                        {
                                            string c = columns[toDelete[j]].Field;
                                            L.Log("[MYSQL VALIDATOR] Extra column `cz_zone_polygon_points`.`" + c + "` found, deleting it.", ConsoleColor.DarkYellow);
                                            query.Append("DROP COLUMN `" + c + "`, ");
                                        }
                                    }

                                    string q2 = query.ToString();
                                    if (q2.Length > 1 && q2[q2.Length - 2] == ',')
                                    {
                                        q2 = q2.Substring(0, q2.Length - 2) + ";";
                                    }

                                    NonQuery(q2, null);
                                }
                                L.Log("[MYSQL VALIDATOR] Table `cz_zone_polygon_points` validated.", ConsoleColor.DarkYellow);
                                break;
                        }
                    }
                    else
                    {
                        NonQuery(MySqlConstants.CREATE_CODE[i], null);
                        L.Log("[MYSQL VALIDATOR] Table `" + tblName + "` created.", ConsoleColor.DarkYellow);
                    }
                }
                catch (Exception ex)
                {
                    L.LogError(ex);
                    L.LogError("Failed to " + (exists ? "verify table integrity of `" : "create `") + tblName + "`. Check above.");
                    bad = true;
                }
            }
        }
        catch (Exception ex)
        {
            L.LogError(ex);
            L.LogError("Failed to verify table integrety, check above. Unloading until this is resolved.");
            bad = true;
        }
        if (bad)
        {
            throw new ZoneAPIException();
        }
        return !bad;
    }

    bool hasVerified = false;
    public void Reload()
    {
        if (!hasVerified)
        {
            if (!VerifyTableIntegrety()) return;
            hasVerified = true;
        }

        List<ZoneModel> mdls = new List<ZoneModel>(_zones.Count);
        _zones.Clear();
        Query("SELECT `pk`, `Name`, `ShortName`, `X`, `Z`, `MinHeight`, `MaxHeight`, `Type`, `UsesMapCoords` FROM `cz_zone_data`;", null, R =>
        {
            ZoneModel mdl = new ZoneModel
            {
                OptPrimaryKey = R.GetInt32(0),
                Name = R.GetString(1),
                ShortName = R.GetStringOrNull(2),
                X = R.GetFloat(3),
                Z = R.GetFloat(4),
                MinimumHeight = R.IsDBNull(5) ? float.NaN : R.GetFloat(5),
                MaximumHeight = R.IsDBNull(6) ? float.NaN : R.GetFloat(6),
                UseMapCoordinates = R.GetBoolean(8)
            };
            EZoneType t = (EZoneType)R.GetByte(7);
            if (t != EZoneType.CIRCLE && t != EZoneType.RECTANGLE && t != EZoneType.POLYGON)
            {
                throw new ZoneReadException("Zone type " + t.ToString() + " is not a valid zone type. Should be: " + 
                                            EZoneType.CIRCLE.ToString() + " (" + ((byte)EZoneType.CIRCLE).ToString() + " | " +
                                            EZoneType.RECTANGLE.ToString() + " (" + ((byte)EZoneType.RECTANGLE).ToString() + " | " +
                                            EZoneType.POLYGON.ToString() + " (" + ((byte)EZoneType.POLYGON).ToString() + " | ");
            }
            mdls.Add(mdl);
        });
        Query("SELECT `ZonePk`, `Radius` FROM `cz_zone_circles`;", null, R =>
        {
            int pk = R.GetInt32(0);
            for (int i = 0; i < mdls.Count; ++i)
            {
                if (mdls[i].OptPrimaryKey == pk)
                {
                    ZoneModel mdl = mdls[i];
                    mdl.ZoneData.Radius = R.GetFloat(1);
                    mdls[i] = mdl;
                    break;
                }
            }
        });
        Query("SELECT `ZonePk`, `SizeX`, `SizeZ` FROM `cz_zone_rectangles`;", null, R =>
        {
            int pk = R.GetInt32(0);
            for (int i = 0; i < mdls.Count; ++i)
            {
                if (mdls[i].OptPrimaryKey == pk)
                {
                    ZoneModel mdl = mdls[i];
                    mdl.ZoneData.SizeX = R.GetFloat(1);
                    mdl.ZoneData.SizeZ = R.GetFloat(2);
                    mdls[i] = mdl;
                    break;
                }
            }
        });
        Query("SELECT `ZonePk`, `X`, `Z` FROM `cz_zone_polygon_points`;", null, R =>
        {
            int pk = R.GetInt32(0);
            for (int i = 0; i < mdls.Count; ++i)
            {
                if (mdls[i].OptPrimaryKey == pk)
                {
                    ZoneModel mdl = mdls[i];
                    bool f = mdl.TempPointList == null;
                    if (f) mdl.TempPointList = new List<Vector2>(8);

                    mdl.TempPointList!.Add(new Vector2(R.GetFloat(1), R.GetFloat(2)));
                    if (f) mdls[i] = mdl;
                    break;
                }
            }
        });
        Query("SELECT `ZonePk`, `Tag` FROM `cz_zone_tags`;", null, R =>
        {
            int pk = R.GetInt32(0);
            for (int i = 0; i < mdls.Count; ++i)
            {
                if (mdls[i].OptPrimaryKey == pk)
                {
                    ZoneModel mdl = mdls[i];
                    bool f = mdl.TempTagList == null;
                    if (f) mdl.TempTagList = new List<string>(8);

                    mdl.TempTagList!.Add(R.GetString(1));
                    if (f) mdls[i] = mdl;
                    break;
                }
            }
        });

        for (int i = mdls.Count - 1; i >= 0; --i)
        {
            ZoneModel mdl = mdls[i];
            if (mdl.TempTagList != null)
                mdl.Tags = mdl.TempTagList.ToArray();
            if (mdl.TempPointList != null)
                mdl.ZoneData.Points = mdl.TempPointList.ToArray();
            try
            {
                mdl.ValidateRead();
                _zones.Add(mdl.GetZone());
            }
            catch (ZoneReadException ex)
            {
                L.LogWarning("Zone read failure for zone id " + ex.Data.OptPrimaryKey + " (" + (ex.Data.Name ?? "null") + ": " + ex.Message);
                mdls.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Underlying MySQL Connection
    /// </summary>
    public MySqlConnection SQL;
    /// <summary>
    /// Should log debug messages.
    /// </summary>
    public bool DebugLogging = false;
    /// <summary>
    /// Data used to log into the database.
    /// </summary>
    protected MySqlData _login;
    private DbDataReader? _currentReader;
    private bool _openSuccess;
    private readonly SemaphoreSlim _threadLocker = new SemaphoreSlim(1, 1);
    /// <inheritdoc />
    public void Dispose()
    {
        // close blocks _threadLocker already, no need to do it here.
        Close();
        SQL.Dispose();
        _threadLocker.Dispose();
    }


    /// <summary>
    /// Open the MySQL connection.
    /// </summary>
    /// <returns>A <see cref="bool"/> meaning whether the operation was successful or not.</returns>
    internal bool Open()
    {
        if (!_threadLocker.Wait(10000))
        {
            L.LogWarning("Failed to wait for the threadlogger and open the MySql connection, all subsequent MySql operations will lock the thread.");
            return false;
        }
        try
        {
            SQL.Open();
            if (DebugLogging) L.Log(nameof(Open) + ": Opened Connection.", ConsoleColor.DarkGray);
            _openSuccess = true;
            _threadLocker.Release();
            return true;
        }
        catch (MySqlException ex)
        {
            _openSuccess = false;
            _threadLocker.Release();
            switch (ex.Number)
            {
                case 0:
                case 1042:
                    L.LogWarning($"MySQL Connection Error: Could not find a host called '{_login.Host}'", ConsoleColor.Yellow);
                    return false;
                case 1045:
                    L.LogWarning($"MySQL Connection Error: Host was found, but login was incorrect.", ConsoleColor.Yellow);
                    return false;
                default:
                    L.LogError($"MySQL Connection Error Code: {ex.Number} - {ex.Message}", ConsoleColor.Yellow);
                    L.LogError(ex);
                    return false;
            }
        }
    }
    /// <summary>
    /// Close the MySQL connection.
    /// </summary>
    /// <returns>A <see cref="bool"/> meaning whether the operation was successful or not.</returns>
    internal bool Close()
    {
        _threadLocker.Wait();
        _openSuccess = false;
        try
        {
            SQL.Close();
            if (DebugLogging) L.Log(nameof(Close) + ": Closed Connection.", ConsoleColor.DarkGray);
            _threadLocker.Release();
            return true;
        }
        catch (MySqlException ex)
        {
            L.LogError("Failed to close MySql Connection synchronously: ");
            L.LogError(ex);
            _threadLocker.Release();
            return false;
        }
    }
    /// <summary>
    /// Open the MySQL connection asynchronously.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing an asynchronous operation that returns a <see cref="bool"/> meaning whether the operation was successful or not.</returns>
    internal async Task<bool> OpenAsync()
    {
        await _threadLocker.WaitAsync();
        try
        {
            await SQL.OpenAsync();
            if (DebugLogging) L.Log(nameof(OpenAsync) + ": Opened Connection.", ConsoleColor.DarkGray);
            _openSuccess = true;
            _threadLocker.Release();
            return true;
        }
        catch (MySqlException ex)
        {
            _openSuccess = false;
            _threadLocker.Release();
            switch (ex.Number)
            {
                case 0:
                case 1042:
                    L.LogWarning($"MySQL Connection Error: Could not find a host called '{_login.Host}'", ConsoleColor.Yellow);
                    return false;
                case 1045:
                    L.LogWarning($"MySQL Connection Error: Host was found, but login was incorrect.", ConsoleColor.Yellow);
                    return false;
                default:
                    L.LogError($"MySQL Connection Error Code: {ex.Number} - {ex.Message}", ConsoleColor.Yellow);
                    L.LogError(ex);
                    return false;
            }
        }
    }
    /// <summary>
    /// Close the MySQL connection asynchronously.
    /// </summary>
    /// <returns>A <see cref="bool"/> meaning whether the operation was successful or not.</returns>
    internal async Task<bool> CloseAsync()
    {
        await _threadLocker.WaitAsync();
        _openSuccess = false;
        try
        {
            await SQL.CloseAsync();
            if (DebugLogging) L.Log(nameof(CloseAsync) + ": Closed Connection.", ConsoleColor.DarkGray);
            _threadLocker.Release();
            return true;
        }
        catch (MySqlException ex)
        {
            L.LogError("Failed to close MySql Connection asynchronously: ");
            L.LogError(ex);
            _threadLocker.Release();
            return false;
        }
    }
    /// <summary>
    /// Call a query, such as a select, etc, and run code for each row.
    /// </summary>
    /// <param name="query">MySQL query to call.</param>
    /// <param name="parameters">MySQL parameters, could be any type. Are represeted in the command by "@index", for example "@0", "@1", etc.</param>
    /// <param name="readLoopAction">Callback to call for each row, with signature: <code><see cref="void"/> <paramref name="readLoopAction"/>(<see cref="MySqlDataReader"/> reader)</code>
    /// To break the loop use the overload: <see cref="Query(string, object[], BreakableReadLoopAction, byte)"/>.</param>
    /// <param name="t">Ignore, used for recursive loop prevention. Set to 1 to avoid recurisve retry.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> == <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the the operation fails and restarting the SQL connection doesn't work.</exception>
    /// <exception cref="Exception">Thrown if the SQL operation fails.</exception>
    internal void Query(string query, object[]? parameters, ReadLoopAction readLoopAction, byte t = 0)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (!_openSuccess && !Open()) throw new Exception("Not connected");
        _threadLocker.Wait();
        using (MySqlCommand Q = new MySqlCommand(query, SQL))
        {
            try
            {
                if (parameters != null) for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                if (DebugLogging) L.Log(nameof(Query) + ": " + Q.CommandText + " : " + (parameters == null ? string.Empty : string.Join(",", parameters)), ConsoleColor.DarkGray);
                using (_currentReader = Q.ExecuteReader())
                {
                    if (_currentReader is MySqlDataReader R)
                    {
                        int row = 0;
                        while (R.Read())
                        {
                            try
                            {
                                readLoopAction.Invoke(R);
                            }
                            catch (Exception ex)
                            {
                                L.LogError("Error in defined reader loop on row " + row + ": ");
                                L.LogError(ex);
                                L.LogError(Environment.StackTrace);
                            }
                            ++row;
                        }
                    }
                    _currentReader.Close();
                    _currentReader.Dispose();
                    Q.Dispose();
                    _currentReader = null;
                }
            }
            catch (InvalidOperationException ex) when (t == 0)
            {
                _threadLocker.Release();
                Close();
                if (Open())
                {
                    Query(query, parameters, readLoopAction, 1);
                }
                else
                {
                    L.LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {(parameters == null ? string.Empty : string.Join(",", parameters))}");
                    L.LogError(ex);
                    throw;
                }
                return;
            }
            catch (Exception ex)
            {
                L.LogError($"Failed to execute command: {Q.CommandText}: {(parameters == null ? string.Empty : string.Join(",", parameters))}");
                L.LogError(ex);
                _threadLocker.Release();
                throw;
            }
        }
        _threadLocker.Release();
    }
    /// <summary>
    /// Call a query, such as a select, etc, and run code for each row.
    /// </summary>
    /// <param name="query">MySQL query to call.</param>
    /// <param name="parameters">MySQL parameters, could be any type. Are represeted in the command by "@index", for example "@0", "@1", etc.</param>
    /// <param name="readLoopAction">Callback to call for each row, with signature: <code><see cref="void"/> <paramref name="readLoopAction"/>(<see cref="MySqlDataReader"/> reader)</code>
    /// To break the loop use the overload: <see cref="QueryAsync(string, object[], BreakableReadLoopAction, byte)"/>.</param>
    /// <param name="t">Ignore, used for recursive loop prevention. Set to 1 to avoid recurisve retry.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous Query operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> == <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the the operation fails and restarting the SQL connection doesn't work.</exception>
    /// <exception cref="Exception">Thrown if the SQL operation fails.</exception>
    internal async Task QueryAsync(string query, object[]? parameters, ReadLoopAction readLoopAction, byte t = 0)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (!_openSuccess && !Open()) throw new Exception("Not connected");
        await _threadLocker.WaitAsync();
        using (MySqlCommand Q = new MySqlCommand(query, SQL))
        {
            try
            {
                if (parameters != null) for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                if (DebugLogging) L.Log(nameof(QueryAsync) + ": " + Q.CommandText + " : " + (parameters == null ? string.Empty : string.Join(",", parameters)), ConsoleColor.DarkGray);
                using (_currentReader = await Q.ExecuteReaderAsync())
                {
                    if (_currentReader is MySqlDataReader R)
                    {
                        int row = 0;
                        while (await R.ReadAsync())
                        {
                            try
                            {
                                readLoopAction.Invoke(R);
                            }
                            catch (Exception ex)
                            {
                                L.LogError("Error in defined reader loop on row " + row + ": ");
                                L.LogError(ex);
                                L.LogError(Environment.StackTrace);
                            }
                            ++row;
                        }
                    }
                    _currentReader.Close();
                    _currentReader.Dispose();
                    Q.Dispose();
                    _currentReader = null;
                }
            }
            catch (InvalidOperationException ex) when (t == 0)
            {
                _threadLocker.Release();
                await CloseAsync();
                if (await OpenAsync())
                {
                    await QueryAsync(query, parameters, readLoopAction, 1);
                    return;
                }
                else
                {
                    L.LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {(parameters == null ? string.Empty : string.Join(",", parameters))}");
                    L.LogError(ex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                L.LogError($"Failed to execute command: {Q.CommandText}: {(parameters == null ? string.Empty : string.Join(",", parameters))}");
                L.LogError(ex);
                _threadLocker.Release();
                throw;
            }
        }
        _threadLocker.Release();
    }
    /// <summary>
    /// Called per row while reading a query response.
    /// </summary>
    internal delegate void ReadLoopAction(MySqlDataReader R);
    /// <summary>
    /// Called per row while reading a query response. Can return <see langword="true"/> to break from the loop.
    /// </summary>
    internal delegate bool BreakableReadLoopAction(MySqlDataReader R);
    /// <summary>
    /// Call a query, such as a select, etc, and run code for each row.
    /// </summary>
    /// <param name="query">MySQL query to call.</param>
    /// <param name="parameters">MySQL parameters, could be any type. Are represeted in the command by "@index", for example "@0", "@1", etc.</param>
    /// <param name="readLoopAction">Callback to call for each row, with signature: <code><see cref="bool"/> <paramref name="readLoopAction"/>(<see cref="MySqlDataReader"/> reader)</code>
    /// Return <see langword="true"/> to break the loop.</param>
    /// <param name="t">Ignore, used for recursive loop prevention. Set to 1 to avoid recurisve retry.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> == <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the the operation fails and restarting the SQL connection doesn't work.</exception>
    /// <exception cref="Exception">Thrown if the SQL operation fails.</exception>
    internal void Query(string query, object[]? parameters, BreakableReadLoopAction readLoopAction, byte t = 0)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (!_openSuccess && !Open()) throw new Exception("Not connected");
        _threadLocker.Wait();
        using (MySqlCommand Q = new MySqlCommand(query, SQL))
        {
            try
            {
                if (parameters != null) for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                if (DebugLogging) L.Log(nameof(Query) + ": " + Q.CommandText + " : " + (parameters == null ? string.Empty : string.Join(",", parameters)), ConsoleColor.DarkGray);
                using (_currentReader = Q.ExecuteReader())
                {
                    if (_currentReader is MySqlDataReader R)
                    {
                        int row = 0;
                        while (R.Read())
                        {
                            try
                            {
                                if (!readLoopAction.Invoke(R)) break;
                            }
                            catch (Exception ex)
                            {
                                L.LogError("Error in defined reader loop on row " + row + ": ");
                                L.LogError(ex);
                                L.LogError(Environment.StackTrace);
                            }
                        }
                        ++row;
                    }
                    _currentReader.Close();
                    _currentReader.Dispose();
                    Q.Dispose();
                    _currentReader = null;
                }
            }
            catch (InvalidOperationException ex) when (t == 0)
            {
                _threadLocker.Release();
                Close();
                if (Open())
                {
                    Query(query, parameters, readLoopAction, 1);
                }
                else
                {
                    L.LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {(parameters == null ? string.Empty : string.Join(",", parameters))}");
                    L.LogError(ex);
                    throw;
                }
                return;
            }
            catch (Exception ex)
            {
                L.LogError($"Failed to execute command: {Q.CommandText}: {(parameters == null ? string.Empty : string.Join(",", parameters))}");
                L.LogError(ex);
                _threadLocker.Release();
                throw;
            }
        }
        _threadLocker.Release();
    }
    /// <summary>
    /// Call a query, such as a select, etc, and run code for each row.
    /// </summary>
    /// <param name="query">MySQL query to call.</param>
    /// <param name="parameters">MySQL parameters, could be any type. Are represeted in the command by "@index", for example "@0", "@1", etc.</param>
    /// <param name="readLoopAction">Callback to call for each row, with signature: <code><see cref="bool"/> <paramref name="readLoopAction"/>(<see cref="MySqlDataReader"/> reader)</code>
    /// Return <see langword="true"/> to break the loop.</param>
    /// <param name="t">Ignore, used for recursive loop prevention. Set to 1 to avoid recurisve retry.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous Query operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> == <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the the operation fails and restarting the SQL connection doesn't work.</exception>
    /// <exception cref="Exception">Thrown if the SQL operation fails.</exception>
    internal async Task QueryAsync(string query, object[]? parameters, BreakableReadLoopAction readLoopAction, byte t = 0)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (!_openSuccess && !Open()) throw new Exception("Not connected");
        await _threadLocker.WaitAsync();
        using (MySqlCommand Q = new MySqlCommand(query, SQL))
        {
            try
            {
                if (parameters != null) for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                if (DebugLogging) L.Log(nameof(QueryAsync) + ": " + Q.CommandText + " : " + (parameters == null ? string.Empty : string.Join(",", parameters)), ConsoleColor.DarkGray);
                using (_currentReader = await Q.ExecuteReaderAsync())
                {
                    if (_currentReader is MySqlDataReader R)
                    {
                        int row = 0;
                        while (await R.ReadAsync())
                        {
                            try
                            {
                                if (!readLoopAction.Invoke(R)) break;
                            }
                            catch (Exception ex)
                            {
                                L.LogError("Error in defined reader loop on row " + row + ": ");
                                L.LogError(ex);
                                L.LogError(Environment.StackTrace);
                            }
                        }
                        ++row;
                    }
                    _currentReader.Close();
                    _currentReader.Dispose();
                    Q.Dispose();
                    _currentReader = null;
                }
            }
            catch (InvalidOperationException ex) when (t == 0)
            {
                _threadLocker.Release();
                await CloseAsync();
                if (await OpenAsync())
                {
                    await QueryAsync(query, parameters, readLoopAction, 1);
                    return;
                }
                else
                {
                    L.LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {(parameters == null ? string.Empty : string.Join(",", parameters))}");
                    L.LogError(ex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                L.LogError($"Failed to execute command: {Q.CommandText}: {(parameters == null ? string.Empty : string.Join(",", parameters))}");
                L.LogError(ex);
                _threadLocker.Release();
                throw;
            }
        }
        _threadLocker.Release();
    }
    /// <summary>
    /// Call a query, such as a select, etc, and get 1 cell's result (gets 0,0 in the table).
    /// </summary>
    /// <param name="query">MySQL query to call.</param>
    /// <param name="parameters">MySQL parameters, could be any type. Are represeted in the command by "@index", for example "@0", "@1", etc.</param>
    /// <param name="converter">Convert from <see cref="object"/> to <typeparamref name="T"/>, usually just a C-cast.</param>
    /// <param name="t">Ignore, used for recursive loop prevention. Set to 1 to avoid recurisve retry.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="query"/> == <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the the operation fails and restarting the SQL connection doesn't work.</exception>
    /// <exception cref="Exception">Thrown if the SQL operation fails.</exception>
    /// <typeparam name="T">Output type</typeparam>
    internal T Scalar<T>(string query, object[]? parameters, Func<object, T> converter, byte t = 0)
    {
        if (query == null) throw new ArgumentNullException(nameof(query));
        if (!_openSuccess && !Open()) throw new Exception("Not connected");
        _threadLocker.Wait();
        using (MySqlCommand Q = new MySqlCommand(query, SQL))
        {
            try
            {
                if (parameters != null) for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
                if (DebugLogging) L.Log(nameof(Scalar) + ": " + Q.CommandText + " : " + (parameters == null ? string.Empty : string.Join(",", parameters)), ConsoleColor.DarkGray);
                object res = Q.ExecuteScalar();
                Q.Dispose();
                _threadLocker.Release();
                if (res == null) return default!;
                else return converter.Invoke(res);
            }
            catch (InvalidOperationException ex) when (t == 0)
            {
                _threadLocker.Release();
                Close();
                if (Open())
                {
                    _threadLocker.Release();
                    return Scalar(query, parameters, converter, 1);
                }
                else
                {
                    L.LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {(parameters == null ? string.Empty : string.Join(",", parameters))}");
                    L.LogError(ex);
                    _threadLocker.Release();
                    throw;
                }
            }
            catch (Exception ex)
            {
                L.LogError($"Failed to execute command: {Q.CommandText}: {(parameters == null ? string.Empty : string.Join(",", parameters))}");
                L.LogError(ex);
                _threadLocker.Release();
                throw;
            }
        }
    }
    /// <summary>
    /// Call a non-query, such as an insert, delete, etc.
    /// </summary>
    /// <param name="command">MySQL non-query to call.</param>
    /// <param name="parameters">MySQL parameters, could be any type. Are represeted in the command by "@index", for example "@0", "@1", etc.</param>
    /// <param name="t">Ignore, used for recursive loop prevention. Set to 1 to avoid recurisve retry.</param>
    /// <returns>The number of rows modified.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="command"/> == <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the the operation fails and restarting the SQL connection doesn't work.</exception>
    /// <exception cref="Exception">Thrown if the SQL operation fails.</exception>
    internal int NonQuery(string command, object[]? parameters, byte t = 0)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (!_openSuccess && !Open()) throw new Exception("Not connected");
        _threadLocker.Wait();
        using (MySqlCommand Q = new MySqlCommand(command, SQL))
        {
            if (parameters != null) for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
            if (DebugLogging) L.Log(nameof(NonQuery) + ": " + Q.CommandText + " : " + (parameters == null ? string.Empty : string.Join(",", parameters)), ConsoleColor.DarkGray);
            try
            {
                int lc = Q.ExecuteNonQuery();
                _threadLocker.Release();
                return lc;
            }
            catch (InvalidOperationException ex) when (t == 0)
            {
                _threadLocker.Release();
                Close();
                if (Open())
                {
                    return NonQuery(command, parameters, 1);
                }
                else
                {
                    L.LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {(parameters == null ? string.Empty : string.Join(",", parameters))}");
                    L.LogError(ex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                L.LogError($"Failed to execute command: {Q.CommandText}: {(parameters == null ? string.Empty : string.Join(",", parameters))}");
                L.LogError(ex);
                _threadLocker.Release();
                throw;
            }
        }
    }
    /// <summary>
    /// Call a non-query, such as an insert, delete, etc.
    /// </summary>
    /// <param name="command">MySQL non-query to call.</param>
    /// <param name="parameters">MySQL parameters, could be any type. Are represeted in the command by "@index", for example "@0", "@1", etc.</param>
    /// <param name="t">Ignore, used for recursive loop prevention. Set to 1 to avoid recurisve retry.</param>
    /// <returns>The number of rows modified.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="command"/> == <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the the operation fails and restarting the SQL connection doesn't work.</exception>
    /// <exception cref="Exception">Thrown if the SQL operation fails.</exception>
    internal async Task<int> NonQueryAsync(string command, object[]? parameters, byte t = 0)
    {
        if (command == null) throw new ArgumentNullException(nameof(command));
        if (!_openSuccess && !Open()) throw new Exception("Not connected");
        await _threadLocker.WaitAsync();
        using (MySqlCommand Q = new MySqlCommand(command, SQL))
        {
            if (parameters != null) for (int i = 0; i < parameters.Length; i++) Q.Parameters.AddWithValue('@' + i.ToString(), parameters[i]);
            if (DebugLogging) L.Log(nameof(NonQueryAsync) + ": " + Q.CommandText + " : " + string.Join(",", parameters), ConsoleColor.DarkGray);
            try
            {
                int lc = await Q.ExecuteNonQueryAsync();
                _threadLocker.Release();
                return lc;
            }
            catch (InvalidOperationException ex) when (t == 0)
            {
                _threadLocker.Release();
                await CloseAsync();
                if (await OpenAsync())
                {
                    return await NonQueryAsync(command, parameters, 1);
                }
                else
                {
                    L.LogError($"Failed reopen the MySql connection to run the command: {Q.CommandText}: {(parameters == null ? string.Empty : string.Join(",", parameters))}");
                    L.LogError(ex);
                    throw;
                }
            }
            catch (Exception ex)
            {
                L.LogError($"Failed to execute command: {Q.CommandText}: {(parameters == null ? string.Empty : string.Join(",", parameters))}");
                L.LogError(ex);
                _threadLocker.Release();
                throw;
            }
        }
    }
}
