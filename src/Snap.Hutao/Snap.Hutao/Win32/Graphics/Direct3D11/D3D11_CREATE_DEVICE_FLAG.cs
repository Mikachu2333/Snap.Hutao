﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Win32.Graphics.Direct3D11;

[Flags]
internal enum D3D11_CREATE_DEVICE_FLAG : uint
{
    D3D11_CREATE_DEVICE_SINGLETHREADED = 0x1U,
    D3D11_CREATE_DEVICE_DEBUG = 0x2U,
    D3D11_CREATE_DEVICE_SWITCH_TO_REF = 0x4U,
    D3D11_CREATE_DEVICE_PREVENT_INTERNAL_THREADING_OPTIMIZATIONS = 0x8U,
    D3D11_CREATE_DEVICE_BGRA_SUPPORT = 0x20U,
    D3D11_CREATE_DEVICE_DEBUGGABLE = 0x40U,
    D3D11_CREATE_DEVICE_PREVENT_ALTERING_LAYER_SETTINGS_FROM_REGISTRY = 0x80U,
    D3D11_CREATE_DEVICE_DISABLE_GPU_TIMEOUT = 0x100U,
    D3D11_CREATE_DEVICE_VIDEO_SUPPORT = 0x800U,
}