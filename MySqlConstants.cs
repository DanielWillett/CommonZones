using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonZones;
internal static class MySqlConstants
{
    internal static readonly string[] TABLES = new string[5]
    {
        "cz_zone_data",
        "cz_zone_tags",
        "cz_zone_circles",
        "cz_zone_rectangles",
        "cz_zone_polygon_points"
    };
    internal static readonly string[] CREATE_CODE = new string[5]
    {
@"CREATE TABLE `cz_zone_data` (
    `pk` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
    `Name` VARCHAR(128) NOT NULL COLLATE 'utf8_unicode_ci',
    `ShortName` VARCHAR(64) NULL DEFAULT NULL COLLATE 'utf8_unicode_ci',
    `X` FLOAT NOT NULL,
    `Z` FLOAT NOT NULL,
    `MinHeight` FLOAT NULL DEFAULT NULL,
    `MaxHeight` FLOAT NULL DEFAULT NULL,
    `Type` TINYINT(3) UNSIGNED NOT NULL,
    `UsesMapCoords` BIT(1) NOT NULL DEFAULT 'b\'0\'',
    PRIMARY KEY (`pk`) USING BTREE,
    UNIQUE INDEX `Name` (`Name`) USING BTREE
)
COLLATE='utf8_unicode_ci'
ENGINE=InnoDB
;",
@"CREATE TABLE `cz_zone_tags` (
	`pk` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
	`ZonePk` INT(10) UNSIGNED NOT NULL,
	`Tag` VARCHAR(256) NOT NULL COLLATE 'utf8_unicode_ci',
	PRIMARY KEY (`pk`) USING BTREE,
	INDEX `TagsZoneRef` (`ZonePk`) USING BTREE,
	CONSTRAINT `TagsZoneRef` FOREIGN KEY (`ZonePk`) REFERENCES `ucwarfare`.`cz_zone_data` (`pk`) ON UPDATE CASCADE ON DELETE CASCADE
)
COLLATE='utf8_unicode_ci'
ENGINE=InnoDB
;",
@"CREATE TABLE `cz_zone_circles` (
	`ZonePk` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
	`Radius` FLOAT NOT NULL,
	PRIMARY KEY (`ZonePk`) USING BTREE,
	CONSTRAINT `CirclePkRef` FOREIGN KEY (`ZonePk`) REFERENCES `ucwarfare`.`cz_zone_data` (`pk`) ON UPDATE CASCADE ON DELETE CASCADE
)
COLLATE='utf8_unicode_ci'
ENGINE=InnoDB
;",
@"CREATE TABLE `cz_zone_rectangles` (
	`ZonePk` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
	`SizeX` FLOAT NOT NULL,
	`SizeZ` FLOAT NOT NULL,
	PRIMARY KEY (`ZonePk`) USING BTREE,
	CONSTRAINT `RectZoneRef` FOREIGN KEY (`ZonePk`) REFERENCES `ucwarfare`.`cz_zone_data` (`pk`) ON UPDATE CASCADE ON DELETE CASCADE
)
COLLATE='utf8_unicode_ci'
ENGINE=InnoDB
;",
@"CREATE TABLE `cz_zone_polygon_points` (
	`pk` INT(10) UNSIGNED NOT NULL AUTO_INCREMENT,
	`ZonePk` INT(10) UNSIGNED NOT NULL,
	`X` FLOAT NOT NULL,
	`Z` FLOAT NOT NULL,
	PRIMARY KEY (`pk`) USING BTREE,
	INDEX `PolygonZoneRef` (`ZonePk`) USING BTREE,
	CONSTRAINT `PolygonZoneRef` FOREIGN KEY (`ZonePk`) REFERENCES `ucwarfare`.`cz_zone_data` (`pk`) ON UPDATE CASCADE ON DELETE CASCADE
)
COLLATE='utf8_unicode_ci'
ENGINE=InnoDB
;"
    };
}

internal readonly struct MySqlColumnData
{
    public readonly string Field;
    public readonly string Type;
    public readonly bool Null;
    public readonly string Key;
    public readonly string? Default;
    public readonly string? Extra;

    public MySqlColumnData(string field, string type, bool @null, string key, string? @default, string? extra)
    {
        Field = field;
        Type = type;
        Null = @null;
        Key = key;
        Default = @default;
        Extra = extra;
    }
}