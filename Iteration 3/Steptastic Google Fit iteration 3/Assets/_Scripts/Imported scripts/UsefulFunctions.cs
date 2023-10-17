using System;
using System.Collections;
using System.Collections.Generic;

public class UsefulFunctions
{
    public class Coordinates
    {
        public double Lat;
        public double Long;
    }


    public static double DistanceTo(double lat1, double lon1, double lat2, double lon2, char unit = 'K')
    {
        double rlat1 = Math.PI*lat1/180;
        double rlat2 = Math.PI*lat2/180;
        double theta = lon1 - lon2;
        double rtheta = Math.PI*theta/180;
        double dist =
            Math.Sin(rlat1)*Math.Sin(rlat2) + Math.Cos(rlat1)*
            Math.Cos(rlat2)*Math.Cos(rtheta);
        dist = Math.Acos(dist);
        dist = dist*180/Math.PI;
        dist = dist*60*1.1515;

        switch (unit)
        {
            case 'K': //Kilometers -> default
                return dist*1.609344;
            case 'N': //Nautical Miles 
                return dist*0.8684;
            case 'M': //Miles
                return dist;
        }

        return dist;
    }

    public static double DistanceTo(Coordinates location1, Coordinates location2, char unit = 'K')
    {
        double rlat1 = Math.PI*location1.Lat/180;
        double rlat2 = Math.PI*location2.Lat / 180;
        double theta = location1.Long - location2.Long;
        double rtheta = Math.PI*theta/180;
        double dist =
            Math.Sin(rlat1)*Math.Sin(rlat2) + Math.Cos(rlat1)*
            Math.Cos(rlat2)*Math.Cos(rtheta);
        dist = Math.Acos(dist);
        dist = dist*180/Math.PI;
        dist = dist*60*1.1515;

        switch (unit)
        {
            case 'K': //Kilometers -> default
                return dist*1.609344;
            case 'N': //Nautical Miles 
                return dist*0.8684;
            case 'M': //Miles
                return dist;
        }

        return dist;
    }




    //for map image

    //may need to update these values. test more
    public static int getMapZoomApproximation()
    {
        int dist = (int)PlayerPrefsX.GetFloat(PlayerPrefsLocations.User.Challenge.ChallengeData.totalDistanceToTarget);

        //Debug.Log("[UsefulFunctions]", () => dist);

        if (dist <= 75)
        {
            return 6;
        }
        else if (dist <= 800)
        {
            return 5;
        }
        if (dist <= 2000)
        {
            return 4;
        }
        else if (dist <= 4500)
        {
            return 3;
        }
        else if (dist <= 8000)
        {
            return 2;
        }
        else
        {
            return 1;//maybe
        }
    }

    public static Tuple<float, float> latLongBetweenTwoLatLongs(float lat1, float long1, float lat2, float long2, float per)
    {
        per /= 100;

        float lat = lat1 + (lat2 - lat1) * per;
        float lng = long1 + (long2 - long1) * per;

        Debug.Log("[UsefulFunctions] lat long between lat longs: " + lat + "," + lng);

        return Tuple.Create(lat, lng);
    }
}