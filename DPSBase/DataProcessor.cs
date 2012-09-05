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
using System.IO;
using System.ComponentModel.Composition;

namespace DPSBase
{
    [InheritedExport(typeof(DataProcessor))]
    public abstract class DataProcessor
    {
        protected static T GetInstance<T>() where T : DataProcessor
        {
            //this forces helper static constructor to be called and gets us an instance if composition worked
            var instance = DPSManager.GetDataProcessor<T>() as T;

            if (instance == null)
            {
                //if the instance is null the type was not added as part of composition
                //create a new instance of T and add it to helper as a compressor

                instance = typeof(T).GetConstructor(new Type[] { }).Invoke(new object[] { }) as T;
                DPSManager.AddDataProcessor(instance);
            }

            return instance;
        }

        /// <summary>
        /// Returns a unique identifier for the compressor type. Used in automatic serialization/compression detection
        /// </summary>
        public abstract byte Identifier { get; }

        /// <summary>
        /// Processes data held in a stream and outputs it to another stream
        /// </summary>
        /// <param name="inStream">An input stream containing data to be processed</param>
        /// <param name="outStream">An output stream to which the processed data is written</param>
        /// <param name="options">Options dictionary for serialisation/data processing</param>
        /// <param name="writtenBytes">The size of the data written to the output stream</param>        
        public abstract void ForwardProcessDataStream(Stream inStream, Stream outStream, Dictionary<string, string> options, out long writtenBytes);

        /// <summary>
        /// Processes data, in reverse, that is held in a stream and outputs it to another stream
        /// </summary>
        /// <param name="inStream">An input stream containing data to be processed</param>
        /// <param name="outStream">An output stream to which the processed data is written</param>
        /// <param name="options">Options dictionary for serialisation/data processing</param>
        /// <param name="writtenBytes">The size of the data written to the output stream</param>                
        public abstract void ReverseProcessDataStream(Stream inStream, Stream outStream, Dictionary<string, string> options, out long writtenBytes);
    }
}
