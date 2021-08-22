﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace FiveDChessDataInterface.MemoryHelpers
{
    static class MemoryUtil
    {
        [Obsolete("use the overload which accepts byte?[] as a paramter instead!")]
        private static Dictionary<IntPtr, byte[]> FindMemoryInternal(IntPtr gameHandle, IntPtr start, uint length, byte[] bytesToFind, bool treatNop90AsWildcard)
            => FindMemoryInternal(gameHandle, start, length, bytesToFind.Select(x => (treatNop90AsWildcard && x == 0x90) ? (byte?)null : x).ToArray());

        private static Dictionary<IntPtr, byte[]> FindMemoryInternal(IntPtr gameHandle, IntPtr start, uint length, byte?[] bytesToFind)
        {
            var foundElements = new Dictionary<IntPtr, byte[]>();

            var bytes = KernelMethods.ReadMemory(gameHandle, start, length);

            int index2 = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                var currByte = bytes[i];
                if (!bytesToFind[index2].HasValue || bytesToFind[index2].Value == currByte)
                {
                    index2++;
                }
                else
                {
                    index2 = 0;
                    continue;
                }

                if (index2 >= bytesToFind.Length)
                {
                    var inArrOffset = i - index2 + 1;
                    foundElements.Add(IntPtr.Add(start, inArrOffset), bytes.Skip(inArrOffset).Take(bytesToFind.Length).ToArray());

                    index2 = 0;
                }
            }

            return foundElements;
        }

        [Obsolete]
        internal static List<IntPtr> FindMemory(IntPtr gameHandle, IntPtr start, uint length, byte[] bytesToFind)
            => FindMemoryInternal(gameHandle, start, length, bytesToFind, false).Keys.ToList();

        [Obsolete]
        internal static Dictionary<IntPtr, byte[]> FindMemoryWithWildcards(IntPtr gameHandle, IntPtr start, uint length, byte[] bytesToFind)
            => FindMemoryInternal(gameHandle, start, length, bytesToFind, true);

        internal static Dictionary<IntPtr, byte[]> FindMemoryWithWildcards(IntPtr gameHandle, IntPtr start, uint length, byte?[] bytesToFind)
            => FindMemoryInternal(gameHandle, start, length, bytesToFind);


        internal static T ReadValue<T>(IntPtr gameHandle, IntPtr location)
        {
            var bytes = KernelMethods.ReadMemory(gameHandle, location, (uint)Marshal.SizeOf<T>());
            var t = typeof(T);

            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Byte:
                    return (dynamic)bytes[0];

                case TypeCode.Int16:
                    return (dynamic)BitConverter.ToInt16(bytes, 0);
                case TypeCode.Int32:
                    return (dynamic)BitConverter.ToInt32(bytes, 0);
                case TypeCode.Int64:
                    return (dynamic)BitConverter.ToInt64(bytes, 0);
                case TypeCode.UInt16:
                    return (dynamic)BitConverter.ToUInt16(bytes, 0);
                case TypeCode.UInt32:
                    return (dynamic)BitConverter.ToUInt32(bytes, 0);
                case TypeCode.UInt64:
                    return (dynamic)BitConverter.ToUInt64(bytes, 0);
                case TypeCode.Single:
                    return (dynamic)BitConverter.ToSingle(bytes, 0);
                case TypeCode.Double:
                    return (dynamic)BitConverter.ToDouble(bytes, 0);

                case TypeCode.Object:
                    switch (t.FullName)
                    {
                        case "System.IntPtr":
                            return (dynamic)new IntPtr(BitConverter.ToInt64(bytes, 0));
                        default:
                            throw new NotImplementedException("Invalid obj type");
                    }

                default:
                    throw new NotImplementedException("Invalid type");
            }
        }

        internal static void WriteValue<T>(IntPtr handle, IntPtr location, T newValue)
        {
            var t = typeof(T);
            byte[] bytesToWrite = null;
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Single:
                case TypeCode.Double:
                    bytesToWrite = BitConverter.GetBytes((dynamic)newValue); break;

                case TypeCode.Object:
                    switch (t.FullName)
                    {
                        case "System.IntPtr":
                            bytesToWrite = BitConverter.GetBytes(((IntPtr)(object)newValue).ToInt64()); break;
                        default:
                            throw new NotImplementedException("Invalid obj type");
                    }
                    break;

                default:
                    throw new NotImplementedException("Invalid type");
            }

            KernelMethods.WriteMemory(handle, location, bytesToWrite);
        }
    }
}
