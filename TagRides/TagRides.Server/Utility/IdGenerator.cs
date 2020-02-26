using System;
using System.Collections.Generic;

namespace TagRides.Server.Utility
{
    public static class IdGenerator
    {
        //TODO make this better? Based on the object? Based on the user that is making the id generate?
        //This should work for now

        /// <summary>
        /// Generates a new id for the given object
        /// The id will be different even if the same object is passed in
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static string GenerateNewId(object obj)
        {
            lock (lockObj)
            {
                return (++lastId).ToString();
            }
        }

        static UInt64 lastId = 0;
        static object lockObj = new object();
    }
}