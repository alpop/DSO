﻿using DSO.DataFrames;
using DSO.Exceptions;
using DSO.ScopeControlFrames;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;


namespace DSO.Utilities
{
    /**
       Most commands has their return frames. For example when you send to device "GetParam" data frame, you expect CurrParamDataFrame in response. 
       This class is for be sure that you will have a correct answer for your request.
       Example:

                      //cast return to expected frame                     
          var Ready = (ScopeControlFrames.ScopeReady)new AcknowledgedFrame().GetAcknowledgedFrame
                                //TypeOf Request data frame (in this case request to enter USB Mode)                        
                            (typeof(ScopeControlFrames.EnterUSBScopeMode), 
                                      //TypeOf Response (and returned) data frame (in this case response should be ScopeReady data frame) 
                                    typeof(ScopeControlFrames.ScopeReady), this);
    **/
    ///<summary>
    /// Most commands has their return frames. For example when you send to device "GetParam" data frame, you expect CurrParamDataFrame in response. 
    /// This class is for be sure that you will have a corrent answer for your request.
    ///</summary>
    ///
    public class AcknowledgedFrame
    {
        Exception lastEx;
        ///<summary>
        ///Returns response frame from request<br />
        ///
        ///<param name="SendType">Command frame type</param>
        ///<param name="ReturnType">Expected response frame type</param>
        ///<param name="Scope">JyeScope object</param>
        ///</summary>
        ///
        public DataFrame GetAcknowledgedFrame(Type SendType, Type ReturnType, JyeScope Scope)  
        {
            var stopwatch = new Stopwatch();
            stopwatch.Restart();
            while (stopwatch.ElapsedMilliseconds < Scope.TimeoutTime)
            {
                try
                {
                    if (SendType == typeof(GetParameters))
                    {
                        WriteFrame(new ScopeControlFrames.GetParameters(), Scope.SerialPort);
                    }
                    else if(SendType == typeof(GetConfig))
                    {
                        WriteFrame(new ScopeControlFrames.GetConfig(), Scope.SerialPort);
                    }
                    else if (SendType == typeof(EnterUSBScopeMode))
                    {
                        WriteFrame(new ScopeControlFrames.EnterUSBScopeMode(), Scope.SerialPort);
                    }
                    return ReturnFrame(ReturnType, Scope.ShortBuffer);
                }
                catch (InvalidDataFrameException ex)
                {
                    try
                    {
                        return ReturnFrame(ReturnType, Scope.LongBuffer);
                    }
                    catch (InvalidDataFrameException ex2)
                    {
                        lastEx = ex;
                    } 
                }
            }

            //return ReturnFrame(ReturnType, Scope.ShortBuffer, Scope.TimeoutTime);
            //GetAcknowledgedFrame(SendType, ReturnType, Scope);
            return null;

            //stringData = "";
            //foreach (var data in scope.ShortBuffer)
            //{
            //    stringData += data + ",";
            //}
            //stringData.Remove(stringData.Length - 1);
            throw new FrameNotAcknowledgedException($"Timeout while waiting for frame acknowledge: " + SendType.ToString() + ", " + ReturnType.ToString() + Environment.NewLine+ "Add. err: "+lastEx.StackTrace);
        }


        private DataFrame ReturnFrame(Type FrameType, byte[] buffer)
        {
            if (FrameType == typeof(DataFrames.DSO068.CurrConfigDataFrame))
            {
                DataFrames.DSO068.CurrConfigDataFrame CurrConfig = new DataFrames.DSO068.CurrConfigDataFrame(buffer);
                return CurrConfig;
            }
            else if (FrameType == typeof(DataFrames.DSO112.CurrConfigDataFrame))
            {
                DataFrames.DSO112.CurrConfigDataFrame CurrParam = new DataFrames.DSO112.CurrConfigDataFrame(buffer);
                return CurrParam;
            }
            else if (FrameType == typeof(CurrParamDataFrame))
            {
                CurrParamDataFrame CurrParam = new CurrParamDataFrame(buffer);
                return CurrParam;
            }
            else if (FrameType == typeof(DataBlockDataFrame))
            {
                DataBlockDataFrame CurrData = new DataBlockDataFrame(buffer);
                return CurrData;
            }
            else if (FrameType == typeof(DataSampleDataFrame))
            {
                DataSampleDataFrame CurrData = new DataSampleDataFrame(buffer);
                return CurrData;
            }
            else if (FrameType == typeof(ScopeControlFrames.ScopeReady))
            {
                ScopeControlFrames.ScopeReady ready = new ScopeControlFrames.ScopeReady(buffer);
                return ready;
            }
            else
            {
                throw new InvalidOperationException("Wrong object type");
            }
        }

        private bool WriteFrame(DataFrame frame, IStreamResource port)
        {
            //Monitor.Enter(port);
            port.Write(frame.Data, 0, frame.Data.Count());
            //Monitor.Exit(port);
            return true;
        }
    }
}
