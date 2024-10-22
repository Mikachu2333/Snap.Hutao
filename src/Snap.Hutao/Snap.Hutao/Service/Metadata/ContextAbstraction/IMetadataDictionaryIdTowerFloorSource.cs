﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Snap.Hutao.Model.Metadata.Tower;
using Snap.Hutao.Model.Primitive;
using System.Collections.Immutable;

namespace Snap.Hutao.Service.Metadata.ContextAbstraction;

internal interface IMetadataDictionaryIdTowerFloorSource
{
    ImmutableDictionary<TowerFloorId, TowerFloor> IdTowerFloorMap { get; set; }
}