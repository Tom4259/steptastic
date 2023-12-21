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


    //calculates the distance between 2 lat-longs and returns in given units, default is kilometer
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

    //calculates the distance between 2 lat-longs and returns in given units, default is kilometer, but using an object for cleaner code
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


    //may need to update these values. test more
    //calculates a value for the zoom size of the map image
    public static int GetMapZoomApproximation()
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

    //returns a lat-long from a percentage between 2 lat longs
    public static Tuple<float, float> LatLongBetweenTwoLatLongs(float lat1, float long1, float lat2, float long2, float per)
    {
        per /= 100;

        float lat = lat1 + (lat2 - lat1) * per;
        float lng = long1 + (long2 - long1) * per;

        Debug.Log("[UsefulFunctions] lat long between lat longs: " + lat + "," + lng);

        return Tuple.Create(lat, lng);
    }


    //calculates the average datetime between 2 date times
    public static DateTime AverageDateBetweenDateTimes(List<DateTime> dates)
    {
        var count = dates.Count;

        double temp = 0D;

        for (int i = 0; i < count; i++)
        {
            temp += dates[i].Ticks / (double)count;
        }

        return new DateTime((long)temp);
    }

    //maps a float between 1 range to a different range
    public static float Map(float value, float a1, float a2, float b1, float b2)
    {
        return b1 + (value - a1) * (b2 - b1) / (a2 - a1);
    }

    //adds an ordinal onto a number
    public static string AddOrdinal(int num)
    {
        if (num <= 0) return num.ToString();

        switch (num % 100)
        {
            case 11:
            case 12:
            case 13:
                return num + "th";
        }

        switch (num % 10)
        {
            case 1:
                return num + "st";
            case 2:
                return num + "nd";
            case 3:
                return num + "rd";
            default:
                return num + "th";
        }
    }

    //formats a number into a more readable number
    public static string ThousandToK(int num)
    {
		return((double)num / 1000).ToString("0.#k");
	}


    //returns the start of the week datetime
    public static DateTime StartOfWeek()
    {
        return DateTime.Today.AddDays(-((int)DateTime.Today.DayOfWeek - 1));
	}
}