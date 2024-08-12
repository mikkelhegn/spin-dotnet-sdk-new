// Generated by `wit-bindgen` 0.29.0. DO NOT EDIT!
// <auto-generated />
#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SpinHttpWorld.wit.imports.wasi.io.v0_2_0
{
    public static class PollInterop {

        internal static class PollWasmInterop
        {
            [DllImport("wasi:io/poll@0.2.0", EntryPoint = "poll"), WasmImportLinkage]
            internal static extern void wasmImportPoll(nint p0, int p1, nint p2);

        }

        public  static unsafe uint[] Poll(List<global::SpinHttpWorld.wit.imports.wasi.io.v0_2_0.IPoll.Pollable> @in)
        {

            byte[] buffer = new byte[4 * @in.Count];
            var gcHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var address = gcHandle.AddrOfPinnedObject();

            for (int index = 0; index < @in.Count; ++index) {
                global::SpinHttpWorld.wit.imports.wasi.io.v0_2_0.IPoll.Pollable element = @in[index];
                int basePtr = (int)address + (index * 4);
                var handle = element.Handle;
                BitConverter.TryWriteBytes(new Span<byte>((void*)(basePtr + 0), 4), unchecked((int)handle));

            }

            var retArea = new uint[2];
            fixed (uint* retAreaByte0 = &retArea[0])
            {
                var ptr = (nint)retAreaByte0;
                PollWasmInterop.wasmImportPoll((int)address, @in.Count, ptr);

                var array = new uint[BitConverter.ToInt32(new Span<byte>((void*)(ptr + 4), 4))];
                new Span<uint>((void*)(BitConverter.ToInt32(new Span<byte>((void*)(ptr + 0), 4))), BitConverter.ToInt32(new Span<byte>((void*)(ptr + 4), 4))).CopyTo(new Span<uint>(array));
                gcHandle.Free();
                return array;
            }

            //TODO: free alloc handle (interopString) if exists
        }

    }
}
