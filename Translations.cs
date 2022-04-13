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
        new TranslationListEntry("zone_syntax", "{{color=#ff8c69}}Syntax: /zone {{visualize|go}}{{/color}}"),
        new TranslationListEntry("zone_visualize_no_results", "{{color=#ff8c69}}You aren't in any existing zone.{{/color}}"),
        new TranslationListEntry("zone_go_no_results", "{{color=#ff8c69}}Couldn't find a zone by that name.{{/color}}"),
        new TranslationListEntry("zone_visualize_success", "{{color=#e6e3d5}}Spawned {0} particles around {{color=#cedcde}}{1}{{/color}}.{{/color}}"),
        new TranslationListEntry("enter_zone_test", "{{color=#e6e3d5}}You've entered the zone {{color=#cedcde}}{0}{{/color}}.{{/color}}"),
        new TranslationListEntry("exit_zone_test", "{{color=#e6e3d5}}You've exited the zone {{color=#cedcde}}{0}{{/color}}.{{/color}}"),
    };
}
