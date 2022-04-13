using CommonZones.Zones;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CommonZones;
internal class ZonePlayerComponent : MonoBehaviour
{
    private float _lastCheck;
    private Player player = null!;
    private readonly List<string> _zones = new List<string>(2); // store names as to not mess up references when the zone file is reloaded.
    private Vector3 _lastPos = Vector3.positiveInfinity;
    private readonly List<Zone> enterQueue = new List<Zone>(2);
    internal void Init(Player player)
    {
        ThreadUtil.assertIsGameThread();
        this.player = player;
        Update();
    }
    private void Update()
    {
        float time = Time.time;
        if (time - _lastCheck >= CommonZones.I.Configuration.Instance.CheckTimeSeconds)
        {
            _lastCheck = time;
            Vector3 pos = player.GetPosition();
            if (_lastPos == pos) return;
            _lastPos = pos;
            for (int i = 0; i < CommonZones.ZoneProvider.Zones.Count; ++i)
            {
                Zone zone = CommonZones.ZoneProvider.Zones[i];
                if (zone.IsInside(pos))
                {
                    for (int j = 0; j < _zones.Count; ++j)
                    {
                        if (_zones[j].Equals(zone.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            goto next;
                        }
                    }
                    enterQueue.Add(zone); // having a separate queue makes sure that teleport operations don't result in a player being in two zones at once.
                }
                else
                {
                    for (int j = _zones.Count - 1; j >= 0; --j)
                    {
                        if (_zones[j].Equals(zone.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            _zones.RemoveAt(j);
                            API.Zones.OnExit(player, zone);
                            break;
                        }
                    }
                }
                next:;
            }
            if (enterQueue.Count > 0)
            {
                for (int i = 0; i < enterQueue.Count; ++i)
                {
                    Zone z = enterQueue[i];
                    API.Zones.OnEnter(player, z);
                    _zones.Add(z.Name);
                }
                enterQueue.Clear();
            }
        }
    }
    private void OnDestroy()
    {
        for (int j = 0; j < _zones.Count; ++j)
        {
            string z = _zones[j];
            for (int i = 0; i < CommonZones.ZoneProvider.Zones.Count; ++i)
            {
                if (CommonZones.ZoneProvider.Zones[i].Name.Equals(z, StringComparison.OrdinalIgnoreCase))
                {
                    API.Zones.OnExit(player, CommonZones.ZoneProvider.Zones[i]);
                    break;
                }
            }
        }
        _zones.Clear();
    }
}
