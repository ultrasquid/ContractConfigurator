﻿using ContractConfigurator.Parameters;
using Contracts;
using SCANsat;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ContractConfigurator;
using System;
using UnityEngine;

namespace ContractConfigurator.SCANsat
{
    public class SCANsatCoverage : Contracts.ContractParameter
    {
        protected string title { get; set; }
        public double coverage { get; set; }
        public CelestialBody targetBody { get; set;}
        public SCANdata.SCANtype scanType { get; set; }

        private float lastRealUpdate = 0.0f;
        private double lastGameTimeUpdate = 0.0;
        private int consecutive_successes = 0;
        private const float REAL_UPDATE_FREQUENCY = 5.0f;
        private const double GAME_UPDATE_FREQUENCY = 100.0;
        private const int CONSECUTIVE_SUCCESSES_REQUIRED = 2;
        private double currentCoverage = 0.0;

        private Dictionary<string, string> nameRemap = new Dictionary<string, string>();
        private TitleTracker titleTracker = new TitleTracker();

        public SCANsatCoverage()
            : this(95.0f, SCANdata.SCANtype.Altimetry, null, "")
        {
            // Re-label a couple of scan names to make them nicer
            nameRemap["AltimetryLoRes"] = "Low resolution altimetry";
            nameRemap["AltimetryHiRes"] = "High resolution altimetry";
        }

        public SCANsatCoverage(double coverage, SCANdata.SCANtype scanType, CelestialBody targetBody, string title)
            : base()
        {
            this.title = title;
            this.coverage = coverage;
            this.scanType = scanType;
            this.targetBody = targetBody;
        }

        protected override string GetTitle()
        {
            string output;
            if (string.IsNullOrEmpty(title))
            {
                string scanTypeName = nameRemap.ContainsKey(scanType.ToString()) ? nameRemap[scanType.ToString()] : scanType.ToString();
                output = scanTypeName + " scan of " + targetBody.PrintName() + ": ";
                if (currentCoverage > 0.0 && state != ParameterState.Complete)
                {
                    output += currentCoverage.ToString("N0") + "% / ";
                }
                output += coverage.ToString("N0") + "%";
                titleTracker.Add(output);
            }
            else
            {
                output = title;
            }

            return output;
        }

        protected override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            node.AddValue("title", title);
            node.AddValue("coverage", coverage);
            node.AddValue("scanType", scanType);
            node.AddValue("targetBody", targetBody.name);
        }

        protected override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            title = node.GetValue("title");
            coverage = ConfigNodeUtil.ParseValue<double>(node, "coverage");
            scanType = ConfigNodeUtil.ParseValue<SCANdata.SCANtype>(node, "scanType");
            targetBody = ConfigNodeUtil.ParseValue<CelestialBody>(node, "targetBody");
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (targetBody == null)
            {
                return;
            }

            // Do a check if either:
            //   REAL_UPDATE_FREQUENCY of real time has elapsed
            //   GAME_UPDATE_FREQUENCY of game time has elapsed
            if (UnityEngine.Time.fixedTime - lastRealUpdate > REAL_UPDATE_FREQUENCY ||
                Planetarium.GetUniversalTime() - lastGameTimeUpdate > GAME_UPDATE_FREQUENCY)
            {
                lastRealUpdate = UnityEngine.Time.fixedTime;
                lastGameTimeUpdate = Planetarium.GetUniversalTime();
                currentCoverage = SCANUtil.GetCoverage((int)scanType, targetBody);

                // Count the number of sucesses
                if (currentCoverage > coverage)
                {
                    consecutive_successes++;
                }
                else
                {
                    consecutive_successes = 0;
                }

                // We've had enough successes to be sure that the scan is complete
                if (consecutive_successes >= CONSECUTIVE_SUCCESSES_REQUIRED)
                {
                    SetComplete();
                }

                // Update contract window
                titleTracker.UpdateContractWindow(GetTitle());
            }
        }
    }
}
