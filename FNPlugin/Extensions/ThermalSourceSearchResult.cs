using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FNPlugin.Extensions
{
    public class ThermalSourceSearchResult
    {
        public ThermalSourceSearchResult(IThermalSource source, float cost)
        {
            Cost = cost;
            Source = source;
        }

        public float Cost { get; private set; }
        public IThermalSource Source { get; private set; }

        public void IncreaseCost(float cost)
        {
            Cost += cost;
        }

        public static ThermalSourceSearchResult BreadthFirstSearchForThermalSource(Part currentpart, int stackdepth, int parentdepth, bool skipSelfContained = false)
        {
            for (int currentDepth = 0; currentDepth <= stackdepth; currentDepth++)
            {
                var source = FindThermalSource(currentpart, currentDepth, parentdepth, skipSelfContained);

                if (source != null)
                    return source;
            }

            return null;
        }

        public static ThermalSourceSearchResult FindThermalSource(Part currentpart, int stackdepth, int parentdepth, bool skipSelfContained)
        {
            if (stackdepth == 0)
            {
                var thermalsources = currentpart.FindModulesImplementing<IThermalSource>();
                var source = skipSelfContained ? thermalsources.FirstOrDefault(s => !s.IsSelfContained) : thermalsources.FirstOrDefault();
                if (source != null)
                    return new ThermalSourceSearchResult(source, 0);
                else
                    return null;
            }

            bool containsNonAndrogynous = currentpart.partInfo.title.Contains("Non-androgynous");
            var containtDockingNode = currentpart.Modules.Contains("ModuleAdaptiveDockingNode");

            int stackDepthCost = containsNonAndrogynous && containtDockingNode ? 0 : 1;

            foreach (var attachNodes in currentpart.attachNodes.Where(atn => atn.attachedPart != null))
            {
                var source = FindThermalSource(attachNodes.attachedPart, (stackdepth - 1), parentdepth, skipSelfContained);

                if (source != null)
                {
                    source.IncreaseCost(stackDepthCost);
                    return source;
                }
            }

            if (parentdepth > 0 && currentpart.parent != null)
            {
                var source = FindThermalSource(currentpart.parent, (stackdepth - 1), (parentdepth - 1), skipSelfContained);

                if (source != null)
                {
                    source.IncreaseCost(2f);
                    return source;
                }
            }

            return null;
        }
    }
}
