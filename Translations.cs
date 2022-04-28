using Rocket.API.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonZones;
public partial class CommonZones
{
    public override TranslationList DefaultTranslations => _defTranslations;
    private static readonly TranslationList _defTranslations = new TranslationList()
    {
        new TranslationListEntry("missing_permission", "{{color=#ff8c69}}You're missing the permissiong \"{0}\"{{/color}}"),
        new TranslationListEntry("zone_syntax", "{{color=#ff8c69}}Syntax: /zone {{visualize|go|edit|list|create|util}}{{/color}}"),
        new TranslationListEntry("zone_visualize_no_results", "{{color=#ff8c69}}You aren't in any existing zone.{{/color}}"),
        new TranslationListEntry("zone_go_no_results", "{{color=#ff8c69}}Couldn't find a zone by that name.{{/color}}"),
        new TranslationListEntry("zone_visualize_success", "{{color=#e6e3d5}}Spawned {0} particles around {{color=#cedcde}}{1}{{/color}}.{{/color}}"),
        new TranslationListEntry("enter_zone_test", "{{color=#e6e3d5}}You've entered the zone {{color=#cedcde}}{0}{{/color}}.{{/color}}"),
        new TranslationListEntry("exit_zone_test", "{{color=#e6e3d5}}You've exited the zone {{color=#cedcde}}{0}{{/color}}.{{/color}}"),
        // zone types
        new TranslationListEntry("zone_type_rectangle", "Rectangle"),
        new TranslationListEntry("zone_type_circle",    "Circle"),
        new TranslationListEntry("zone_type_polygon",   "Polygon"),
        new TranslationListEntry("zone_type_invalid",   "Not Specified"),

        // zone create
        new TranslationListEntry("create_zone_syntax", "{{color=#ff8c69}}Syntax: /zone create {{polygon|rectangle|circle}} {{name}}.{{/color}}"),
        new TranslationListEntry("create_zone_success", "{{color=#e6e3d5}}Started zone builder for {0}, a {1} zone.{{/color}}"),
        new TranslationListEntry("create_zone_name_taken", "{{color=#ff8c69}}\"{0}\" is already in use by another zone.{{/color}}"),
        new TranslationListEntry("create_zone_name_taken_2", "{{color=#ff8c69}}\"{0}\" is already in use by another zone being created by {1}.{{/color}}"),

        // zone edit
        new TranslationListEntry("edit_zone_syntax", "{{color=#ff8c69}}Syntax: /zone edit {{existing|maxheight|minheight|finalize|cancel|addpoint|delpoint|clearpoints|setpoint|orderpoint|radius|sizex|sizez|center|name|shortname|type}} [value]{{/color}}"),
        new TranslationListEntry("edit_zone_not_started", "{{color=#ff8c69}}Start creating a zone with {{color=#ffffff}}/zone create {{polygon|rectangle|circle}} {{name}}{{/color}}.{{/color}}"),
        new TranslationListEntry("edit_zone_finalize_exists", "{{color=#e6e3d5}}There's already a zone saved with that name.{{/color}}"),
        new TranslationListEntry("edit_zone_finalize_success", "{{color=#e6e3d5}}Successfully finalized and saved {0}.{{/color}}"),
        new TranslationListEntry("edit_zone_finalize_success_overwrite", "{{color=#e6e3d5}}Successfully overwrote {0}.{{/color}}"),
        new TranslationListEntry("edit_zone_cancel_success", "{{color=#e6e3d5}}Successfully cancelled making {0}.{{/color}}"),
        new TranslationListEntry("edit_zone_finalize_error", "{{color=#ff8c69}}There was a problem finalizing your zone: \"{0}\".{{/color}}"),
        new TranslationListEntry("edit_zone_maxheight_badvalue", "{{color=#ff8c69}}Maximum Height must be a decimal or whole number, or leave it blank to use the player's current height.{{/color}}"),
        new TranslationListEntry("edit_zone_maxheight_success", "{{color=#e6e3d5}}Set maximum height to {0}.{{/color}}"),
        new TranslationListEntry("edit_zone_minheight_badvalue", "{{color=#ff8c69}}Minimum Height must be a decimal or whole number, or leave it blank to use the player's current height.{{/color}}"),
        new TranslationListEntry("edit_zone_minheight_success", "{{color=#e6e3d5}}Set minimum height to {0}.{{/color}}"),
        new TranslationListEntry("edit_zone_type_badvalue", "{{color=#ff8c69}}Type must be rectangle, circle, or polygon.{{/color}}"),
        new TranslationListEntry("edit_zone_type_already_set", "{{color=#ff8c69}}This zone is already a {0}.{{/color}}"),
        new TranslationListEntry("edit_zone_type_success", "{{color=#e6e3d5}}Set type to {0}.{{/color}}"),
        new TranslationListEntry("edit_zone_addpoint_badvalues", "{{color=#ff8c69}}Adding a point requires X and Z parameters, or leave them blank to use the player's current position.{{/color}}"),
        new TranslationListEntry("edit_zone_addpoint_success", "{{color=#e6e3d5}}Added point #{0} at {1}.{{/color}}"),
        new TranslationListEntry("edit_zone_delpoint_badvalues", "{{color=#ff8c69}}Deleting a point requires either: nearby X and Z parameters, a point number, or leave them blank to use the player's current position.{{/color}}"),
        new TranslationListEntry("edit_zone_point_number_not_point", "{{color=#ff8c69}}Point #{0} is not defined.{{/color}}"),
        new TranslationListEntry("edit_zone_point_none_nearby", "{{color=#ff8c69}}There is no point near {0}.{{/color}}"),
        new TranslationListEntry("edit_zone_delpoint_success", "{{color=#e6e3d5}}Removed point #{0} at {1}.{{/color}}"),
        new TranslationListEntry("edit_zone_setpoint_badvalues", "{{color=#ff8c69}}Moving a point requires either: {{nearby src x}} {{nearby src z}} {{dest x}} {{dest z}}, {{pt num}} (destination is player position), {{pt num}} {{dest x}} {{dest z}}, or {{nearby src x}} {{nearby src z}} (destination is nearby player).{{/color}}"),
        new TranslationListEntry("edit_zone_setpoint_success", "{{color=#e6e3d5}}Moved point #{0} from {1} to {2}.{{/color}}"),
        new TranslationListEntry("edit_zone_radius_badvalue", "{{color=#ff8c69}}Radius must be a decimal or whole number, or leave it blank to use the player's current distance from the center point.{{/color}}"),
        new TranslationListEntry("edit_zone_radius_success", "{{color=#e6e3d5}}Set radius to {0}.{{/color}}"),
        new TranslationListEntry("edit_zone_sizex_badvalue", "{{color=#ff8c69}}Size X must be a decimal or whole number, or leave it blank to use the player's current distance from the center point.{{/color}}"),
        new TranslationListEntry("edit_zone_sizex_success", "{{color=#e6e3d5}}Set size x to {0}.{{/color}}"),
        new TranslationListEntry("edit_zone_sizez_badvalue", "{{color=#ff8c69}}Size Z must be a decimal or whole number, or leave it blank to use the player's current distance from the center point.{{/color}}"),
        new TranslationListEntry("edit_zone_sizez_success", "{{color=#e6e3d5}}Set size z to {0}.{{/color}}"),
        new TranslationListEntry("edit_zone_center_badvalue", "{{color=#ff8c69}}To set center you must provide two decimal or whole numbers, or leave them blank to use the player's current position.{{/color}}"),
        new TranslationListEntry("edit_zone_center_success", "{{color=#e6e3d5}}Set center position to {0}.{{/color}}"),
        new TranslationListEntry("edit_zone_clearpoints_success", "{{color=#e6e3d5}}Cleared all polygon points.{{/color}}"),
        new TranslationListEntry("edit_zone_name_badvalue", "{{color=#ff8c69}}Name requires one string argument. Quotation marks aren't required.{{/color}}"),
        new TranslationListEntry("edit_zone_name_success", "{{color=#e6e3d5}}Set name to \"{0}\".{{/color}}"),
        new TranslationListEntry("edit_zone_short_name_badvalue", "{{color=#ff8c69}}Short name requires one string argument. Quotation marks aren't required.{{/color}}"),
        new TranslationListEntry("edit_zone_short_name_success", "{{color=#e6e3d5}}Set short name to \"{0}\".{{/color}}"),
        new TranslationListEntry("edit_zone_existing_badvalue", "{{color=#ff8c69}}Edit existing zone requires the zone name as a parameter. Alternatively stand in the zone (without overlapping another).{{/color}}"),
        new TranslationListEntry("edit_zone_existing_in_progress", "{{color=#ff8c69}}Cancel or finalize the zone you're currently editing first.{{/color}}"),
        new TranslationListEntry("edit_zone_existing_success", "{{color=#e6e3d5}}Started editing zone {0}, a {1} zone.{{/color}}"),


        // edit zone ui
        new TranslationListEntry("edit_zone_ui_suggested_command_1", "/zone edit maxheight [value]"),
        new TranslationListEntry("edit_zone_ui_suggested_command_2", "/zone edit minheight [value]"),
        new TranslationListEntry("edit_zone_ui_suggested_command_3", "/zone edit finalize"),
        new TranslationListEntry("edit_zone_ui_suggested_command_4", "/zone edit cancel"),
        new TranslationListEntry("edit_zone_ui_suggested_command_5_p", "/zone edit addpt [x z]"),
        new TranslationListEntry("edit_zone_ui_suggested_command_6_p", "/zone edit delpt [number | x z]"),
        new TranslationListEntry("edit_zone_ui_suggested_command_7_p", "/zone edit setpt {{number | src: x z | number dest: x z | src: x z dest: x z}}"),
        new TranslationListEntry("edit_zone_ui_suggested_command_8_p", "/zone edit orderpt {{from-index to-index | to-index | src: x z to-index}}"),
        new TranslationListEntry("edit_zone_ui_suggested_command_9_c", "/zone edit radius [value]"),
        new TranslationListEntry("edit_zone_ui_suggested_command_10_r", "/zone edit sizex [value]"),
        new TranslationListEntry("edit_zone_ui_suggested_command_11_r", "/zone edit sizez [value]"),
        new TranslationListEntry("edit_zone_ui_suggested_command_12", "/zone util location"),
        new TranslationListEntry("edit_zone_ui_suggested_command_13", "/zone edit type {{rectangle | circle | polygon}}"),
        new TranslationListEntry("edit_zone_ui_suggested_command_14_p", "/zone edit clearpoints"),
        new TranslationListEntry("edit_zone_ui_suggested_commands", "Suggested Commands"),
        new TranslationListEntry("edit_zone_ui_y_limits", "Y: {0} - {1}"),
        new TranslationListEntry("edit_zone_ui_y_limits_infinity", "∞"),

        // zone util
        new TranslationListEntry("util_zone_syntax", "{{color=#ff8c69}}Syntax: /zone util {{location}}{{/color}}"),
        new TranslationListEntry("util_zone_location", "{{color=#e6e3d5}}Location: {0}, {1}, {2} | Yaw: {3}°.{{/color}}"),
    };
}
