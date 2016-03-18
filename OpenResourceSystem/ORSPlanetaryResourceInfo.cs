using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace OpenResourceSystem {
    public class ORSPlanetaryResourceInfo 
    {
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

        public double ScaleFactor 
        { 
            get { return scale_factor; }
            set { scale_factor = value; }
        }

        public double ScaleMultiplier 
        { 
            get { return scale_multiplier; }
            set { scale_multiplier = value; }
        }

        public int ResourceScale 
        {
            get { return scale; }
            set { scale = value; }
        }
        
        public ORSPlanetaryResourceInfo(string name, Texture2D map, int body) 
        {
            this.name = name;
            this.map = map;
            this.body = body;
        }

        public double getPixelAbundanceValue(int pix_x, int pix_y) 
        {
            Color pix_color = map.GetPixel(pix_x, pix_y);
            double resource_val = 0;
            double scale_factor = ScaleFactor;
            double scale_multiplier = ScaleMultiplier;
            if (getResourceScale() == ORSPlanetaryResourceInfo.LOG_SCALE) 
            {
                resource_val = Math.Pow(scale_factor, pix_color.grayscale * 255.0) / 1000000 * scale_multiplier;
            } 
            else if (getResourceScale() == ORSPlanetaryResourceInfo.LINEAR_SCALE) 
            {
                resource_val = pix_color.grayscale * scale_multiplier;
            }
            return resource_val;
        }

        public double getLatLongAbundanceValue(double lat, double lng) 
        {
            lat = ORSHelper.ToLatitude(lat);
            lng = ORSHelper.ToLongitude(lng);

            double len_x = map.width;
            double len_y = map.height;
            double origin_x = map.width / 2.0;
            double origin_y = map.height / 2.0;

            double map_x = (lng * len_x / 2 / 180 + origin_x);
            double map_y = (lat * len_y / 2 / 90 + origin_y);

            int pix_x = (int)Math.Round(map_x);
            int pix_y = (int)Math.Round(map_y);

            return getPixelAbundanceValue(pix_x, pix_y);
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
