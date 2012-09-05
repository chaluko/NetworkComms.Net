﻿//  Copyright 2011 Marc Fletcher, Matthew Dean
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition.Hosting;
using DPSBase;
using System.ComponentModel.Composition;
using System.IO;

namespace DPSBase
{
    /// <summary>
    /// Automatically detects and manages the use of DataSerializers and DataProcessors.  Any DataSerializer or DataProcessor in an assembly located in the working directory (including subdirectories) will be automatically detected
    /// </summary>
    public sealed class DPSManager
    {
        class SerializerComparer : IEqualityComparer<DataSerializer>
        {
            public static SerializerComparer Instance { get; private set; }

            static SerializerComparer() { Instance = new SerializerComparer(); }

            public SerializerComparer(){}

            #region IEqualityComparer<ISerialize> Members

            public bool Equals(DataSerializer x, DataSerializer y)
            {
                if (x.Identifier == y.Identifier)
                {
                    Type xType = x.GetType();
                    Type yType = y.GetType();

                    if (xType != yType)
                        throw new InvalidOperationException("Cannot have two different serializer types with same identifier");
                    else
                        return true;
                }
                else
                    return false;
            }

            public int GetHashCode(DataSerializer obj)
            {
                return obj.Identifier.GetHashCode() ^ obj.GetType().GetHashCode();
            }

            #endregion
        }

        class DataProcessorComparer : IEqualityComparer<DataProcessor>
        {
            public static DataProcessorComparer Instance { get; private set; }

            static DataProcessorComparer() { Instance = new DataProcessorComparer(); }

            public DataProcessorComparer() { }

            #region IEqualityComparer<ICompress> Members

            public bool Equals(DataProcessor x, DataProcessor y)
            {
                if (x.Identifier == y.Identifier)
                {
                    Type xType = x.GetType();
                    Type yType = y.GetType();

                    if (xType != yType)
                        throw new InvalidOperationException("Cannot have two different serializer types with same identifier");
                    else
                        return true;
                }
                else
                    return false;
            }

            public int GetHashCode(DataProcessor obj)
            {
                return obj.Identifier.GetHashCode() ^ obj.GetType().GetHashCode();
            }

            #endregion
        }

        private CompositionContainer _container;

        [ImportMany(typeof(DataSerializer))]
        private IEnumerable<DataSerializer> serializers = null;

        [ImportMany(typeof(DataProcessor))]
        private IEnumerable<DataProcessor> compressors = null;

        private Dictionary<Type, DataSerializer> SerializersByType = new Dictionary<Type, DataSerializer>();
        private Dictionary<byte, DataSerializer> SerializersByID = new Dictionary<byte, DataSerializer>();
        private Dictionary<Type, DataProcessor> DataProcessorsByType = new Dictionary<Type, DataProcessor>();
        private Dictionary<byte, DataProcessor> DataProcessorsByID = new Dictionary<byte, DataProcessor>();

        static DPSManager instance;
        
        static DPSManager()
        {
            instance = new DPSManager();
        }

        /// <summary>
        /// Retrieves all <see cref="DataSerializer"/>s known to the DPSManager
        /// </summary>
        /// <returns>A dictionary whose key is the <see cref="System.Type"/> of the <see cref="DataSerializer"/> instance that is the value</returns>
        public static Dictionary<Type, DataSerializer> GetAllDataSerializes()
        {
            return instance.SerializersByType;
        }

        /// <summary>
        /// Retrieves the singleton instance of the <see cref="DataSerializer"/> with <see cref="System.Type"/> T
        /// </summary>
        /// <typeparam name="T">The <see cref="System.Type"/> of the <see cref="DataSerializer"/> to retrieve </typeparam>
        /// <returns>The retrieved singleton instance of the desired <see cref="DataSerializer"/></returns>
        public static DataSerializer GetDataSerializer<T>() where T : DataSerializer
        {
            if (instance.SerializersByType.ContainsKey(typeof(T)))
                return instance.SerializersByType[typeof(T)];
            else
                return null;
        }

        /// <summary>
        /// Retrieves the singleton instance of the <see cref="DataSerializer"/> corresponding to a given id
        /// </summary>
        /// <param name="Id">The identifier corresponding to the desired <see cref="DataSerializer"/></param>
        /// <returns>The retrieved singleton instance of the desired <see cref="DataSerializer"/></returns>
        public static DataSerializer GetSerializer(byte Id)
        {
            if (instance.SerializersByID.ContainsKey(Id))
                return instance.SerializersByID[Id];
            else
                return null;
        }

        /// <summary>
        /// Retrieves all <see cref="DataProcessor"/>s known to the DPSManager
        /// </summary>
        /// <returns>A dictionary whose key is the <see cref="System.Type"/> of the <see cref="DataProcessor"/> instance that is the value</returns>
        public static Dictionary<Type, DataProcessor> GetAllDataProcessors()
        {            
            return instance.DataProcessorsByType;
        }

        /// <summary>
        /// Retrieves the singleton instance of the <see cref="DataProcessor"/> with <see cref="System.Type"/> T
        /// </summary>
        /// <typeparam name="T">The <see cref="System.Type"/> of the <see cref="DataProcessor"/> to retrieve </typeparam>
        /// <returns>The retrieved singleton instance of the desired <see cref="DataProcessor"/></returns>
        public static DataProcessor GetDataProcessor<T>() where T : DataProcessor
        {
            if (instance.DataProcessorsByType.ContainsKey(typeof(T)))
                return instance.DataProcessorsByType[typeof(T)];
            else
                return null;
        }

        /// <summary>
        /// Retrieves the singleton instance of the <see cref="DataProcessor"/> corresponding to a given id
        /// </summary>
        /// <param name="Id">The identifier corresponding to the desired <see cref="DataProcessor"/></param>
        /// <returns>The retrieved singleton instance of the desired <see cref="DataProcessor"/></returns>
        public static DataProcessor GetDataProcessor(byte Id)
        {
            if (instance.DataProcessorsByID.ContainsKey(Id))
                return instance.DataProcessorsByID[Id];
            else
                return null;
        }

        /// <summary>
        /// Allows the addition of <see cref="DataProcessor"/>s which are not autodetected
        /// </summary>
        /// <param name="dataProcessor"></param>
        /// <exception cref="ArgumentException"></exception>
        public static void AddDataProcessor(DataProcessor dataProcessor)
        {
            if (instance.DataProcessorsByType.ContainsKey(dataProcessor.GetType()))
                if (instance.DataProcessorsByType[dataProcessor.GetType()] != dataProcessor)
                    throw new ArgumentException();
                else
                    return;

            instance.DataProcessorsByType.Add(dataProcessor.GetType(), dataProcessor);
            instance.DataProcessorsByID.Add(dataProcessor.Identifier, dataProcessor);
        }

        public static void AddSerializer(DataSerializer dataProcessor)
        {
            if (instance.SerializersByType.ContainsKey(dataProcessor.GetType()))
                if (instance.SerializersByType[dataProcessor.GetType()] != dataProcessor)
                    throw new Exception();
                else
                    return;
            
            instance.SerializersByType.Add(dataProcessor.GetType(), dataProcessor);
            instance.SerializersByID.Add(dataProcessor.Identifier, dataProcessor);
        }

        public static long CreateSerializerDataProcessorIdentifier(DataSerializer serializer, List<DataProcessor> dataProcessors)
        {
            long res = 0;
                        
            res |= serializer.Identifier;
            res <<= 8;

            if (dataProcessors != null && dataProcessors.Count != 0)
            {
                if (dataProcessors.Count > 7)
                    throw new InvalidOperationException("Cannot specify more than 7 data processors for automatic serialization detection");

                for (int i = 0; i < dataProcessors.Count; i++)
                {
                    res |= dataProcessors[i].Identifier;

                    if (i != dataProcessors.Count - 1)
                        res <<= 8;
                }

                if (dataProcessors.Count < sizeof(long) - 1)
                    res <<= (8 * (sizeof(long) - 1 - dataProcessors.Count));
            }
            else
                res <<= (8 * (sizeof(long) - 2));

            return res;
        }

        public static void GetSerializerDataProcessorsFromIdentifier(long id, out DataSerializer serializer, out List<DataProcessor> dataProcessors)
        {
            serializer = GetSerializer((byte)(id >> 56));

            dataProcessors = new List<DataProcessor>();

            for (int i = 6; i >= 0; i--)
            {
                long mask = 0xFF;
                byte processorId = (byte)((id & (mask << (8 * i))) >> (8 * i));

                if (processorId != 0)
                    dataProcessors.Add(GetDataProcessor(processorId));
            }
        }

        private DPSManager()
        {
            //An aggregate catalog that combines multiple catalogs
            var catalog = new AggregateCatalog();

            //We're going to try and guess where we might have serializers and compressors.  We will look in all currently loaded assemblies
            //and all dll and exe files in the application root directory and subdirectories

            //Add all the currently loaded assemblies
            AppDomain.CurrentDomain.GetAssemblies().ToList().ForEach(ass => catalog.Catalogs.Add(new AssemblyCatalog(ass)));

            var allDirectories = Directory.GetDirectories(Directory.GetCurrentDirectory(), "*", SearchOption.AllDirectories);

            //Adds all the parts found in dlls in the same localtion
            catalog.Catalogs.Add(new DirectoryCatalog(Directory.GetCurrentDirectory(), "*.dll"));
            //Adds all the parts found in exe in the same localtion
            catalog.Catalogs.Add(new DirectoryCatalog(Directory.GetCurrentDirectory(), "*.exe"));

            for (int i = 0; i < allDirectories.Length; i++)
            {
                if (Directory.GetFiles(allDirectories[i], "*.exe", SearchOption.TopDirectoryOnly).Union(
                    Directory.GetFiles(allDirectories[i], "*.dll", SearchOption.TopDirectoryOnly)).Count() != 0)
                {
                    //Adds all the parts found in dlls in the same localtion
                    catalog.Catalogs.Add(new DirectoryCatalog(allDirectories[i], "*.dll"));
                    //Adds all the parts found in exe in the same localtion
                    catalog.Catalogs.Add(new DirectoryCatalog(allDirectories[i], "*.exe"));
                }
            }

            _container = new CompositionContainer(catalog);

            //Fill the imports of this object
            try
            {
                _container.ComposeParts(this);
            }
            catch (CompositionException compositionException)
            {
                Console.WriteLine(compositionException.ToString());
            }

            var serializersToAdd = serializers.Distinct(SerializerComparer.Instance).Where(s => !SerializersByType.ContainsKey(s.GetType())).ToArray();
            var dataProcessorsToAdd = compressors.Distinct(DataProcessorComparer.Instance).Where(c => !DataProcessorsByType.ContainsKey(c.GetType())).ToArray();

            foreach (var instance in serializersToAdd)
            {
                SerializersByType.Add(instance.GetType(), instance);
                SerializersByID.Add(instance.Identifier, instance);
            }

            foreach (var instance in dataProcessorsToAdd)
            {
                DataProcessorsByType.Add(instance.GetType(), instance);
                DataProcessorsByID.Add(instance.Identifier, instance);
            }                        
        }
    }
}
