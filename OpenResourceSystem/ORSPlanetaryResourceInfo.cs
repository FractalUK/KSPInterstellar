using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenResourceSystem {
    public class ORSPlanetaryResourceInfo {
        public const int LOG_SCALE = 0;
        public const int LINEAR_SCALE = 1;

        protected Texture2D map;
        protected string name;
        protected int body;
        protected string resourcename = "";
        protected int scale = 0;
        protected double scale_factor = 1;
        protected double scale_multiplier = 1;
        protected string displayTexture = "";
        protected double displayThreshold = 0.001;
        
        public ORSPlanetaryResourceInfo(string name, Texture2D map, int body) {
            this.name = name;
            this.map = map;
            this.body = body;
        }

        public void setDisplayTexture(string texpath) {
            displayTexture = texpath;
        }

        public void setDisplayThreshold(double displayThreshold) {
            this.displayThreshold = displayThreshold;
        }

        public void setResourceName(string resourcename) {
            this.resourcename = resourcename;
        }

        public void setResourceScale(int scale) {
            this.scale = scale;
        }

        public void setScaleFactor(double scale_factor) {
            this.scale_factor = scale_factor;
        }

        public void setScaleMultiplier(double scale_multiplier) {
            this.scale_multiplier = scale_multiplier;
        }

        public void setResourceScale(string scalestr) {
            if(scalestr == "LOG_SCALE") {
                scale = ORSPlanetaryResourceInfo.LOG_SCALE;
            }else if (scalestr == "LINEAR_SCALE") {
                scale = ORSPlanetaryResourceInfo.LINEAR_SCALE;
            }
        }

        public int getBody() {
            return body;
        }

        public string getDisplayTexturePath() {
            return displayTexture;
        }

        public double getDisplayThreshold() {
            return displayThreshold;
        }

        public string getName() {
            return name;
        }
        
        public Texture2D getResourceMap() {
            return map;
        }

        public string getResourceName() {
            return resourcename;
        }

        public int getResourceScale() {
            return scale;
        }

        public double getScaleFactor() {
            return scale_factor;
        }

        public double getScaleMultiplier() {
            return scale_multiplier;
        }
    }
}
