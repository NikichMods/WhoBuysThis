using System;
using BepInEx;
using UnityEngine;

namespace WhoBuysThisResearch
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class WhoBuysThisResearchPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "nikich.gyk.whobuysthis.research";
        public const string PluginName = "Who Buys This? Research Harness";
        public const string PluginVersion = "0.0.0";

        private const float ReadinessCheckSeconds = 0.5f;
        private const float KnownNpcCheckSeconds = 1.0f;
        private const float StableDelaySeconds = 2.0f;

        private DiagnosticCollector _collector;
        private float _nextCheck;
        private float _readySince = -1f;
        private bool _initialDumpDone;
        private ulong _knownFingerprint;
        private int _knownCount;
        private bool _knownChangePending;
        private float _knownChangeDue;

        private void Awake()
        {
            _collector = new DiagnosticCollector(Logger);
            _nextCheck = Time.realtimeSinceStartup + ReadinessCheckSeconds;
            Logger.LogInfo(PluginName + " loaded. Research-only; does not modify gameplay/save state.");
            Logger.LogInfo("WHO_BUYS_THIS_DIAGNOSTIC lifecycle=waiting note=known_npcs_may_be_empty_and_that_is_valid");
        }

        private void Update()
        {
            if (_collector == null || Time.realtimeSinceStartup < _nextCheck) return;
            _nextCheck = Time.realtimeSinceStartup + (_initialDumpDone ? KnownNpcCheckSeconds : ReadinessCheckSeconds);

            object mainGame;
            object save;
            object gameBalance;
            System.Collections.IList worldObjects;
            if (!_collector.IsRuntimeReady(out mainGame, out save, out gameBalance, out worldObjects))
            {
                _readySince = -1f;
                return;
            }

            if (!_initialDumpDone)
            {
                if (_readySince < 0f)
                {
                    _readySince = Time.realtimeSinceStartup;
                    return;
                }
                if (Time.realtimeSinceStartup - _readySince < StableDelaySeconds) return;

                string reportPath;
                if (_collector.Dump("initial-stable-runtime", out reportPath))
                {
                    _initialDumpDone = true;
                    _knownFingerprint = _collector.BuildKnownNpcFingerprint(save, out _knownCount);
                    Logger.LogInfo("WHO_BUYS_THIS_DIAGNOSTIC lifecycle=ready known_npcs=" + _knownCount +
                                   " note=zero_is_valid_ready_state");
                }
                return;
            }

            int currentKnownCount;
            ulong currentFingerprint = _collector.BuildKnownNpcFingerprint(save, out currentKnownCount);
            if (currentFingerprint != _knownFingerprint)
            {
                _knownFingerprint = currentFingerprint;
                _knownCount = currentKnownCount;
                _knownChangePending = true;
                _knownChangeDue = Time.realtimeSinceStartup + StableDelaySeconds;
                Logger.LogInfo("WHO_BUYS_THIS_DIAGNOSTIC known_npc_change_detected count=" + currentKnownCount +
                               " action=debounced_redump");
            }

            if (_knownChangePending && Time.realtimeSinceStartup >= _knownChangeDue)
            {
                _knownChangePending = false;
                string reportPath;
                _collector.Dump("known-npc-change", out reportPath);
            }
        }
    }
}
