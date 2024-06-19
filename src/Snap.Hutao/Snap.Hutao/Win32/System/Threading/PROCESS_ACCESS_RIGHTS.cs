﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

namespace Snap.Hutao.Win32.System.Threading;

[Flags]
public enum PROCESS_ACCESS_RIGHTS : uint
{
    PROCESS_TERMINATE = 1U,
    PROCESS_CREATE_THREAD = 2U,
    PROCESS_SET_SESSIONID = 4U,
    PROCESS_VM_OPERATION = 8U,
    PROCESS_VM_READ = 0x10U,
    PROCESS_VM_WRITE = 0x20U,
    PROCESS_DUP_HANDLE = 0x40U,
    PROCESS_CREATE_PROCESS = 0x80U,
    PROCESS_SET_QUOTA = 0x100U,
    PROCESS_SET_INFORMATION = 0x200U,
    PROCESS_QUERY_INFORMATION = 0x400U,
    PROCESS_SUSPEND_RESUME = 0x800U,
    PROCESS_QUERY_LIMITED_INFORMATION = 0x1000U,
    PROCESS_SET_LIMITED_INFORMATION = 0x2000U,
    PROCESS_ALL_ACCESS = 0x1FFFFFU,
    PROCESS_DELETE = 0x10000U,
    PROCESS_READ_CONTROL = 0x20000U,
    PROCESS_WRITE_DAC = 0x40000U,
    PROCESS_WRITE_OWNER = 0x80000U,
    PROCESS_SYNCHRONIZE = 0x100000U,
    PROCESS_STANDARD_RIGHTS_REQUIRED = 0xF0000U,
}