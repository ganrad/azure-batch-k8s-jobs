﻿// 
// Copyright (c) Microsoft.  All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;

/**
 * Description:
 * This class defines constant values for Azure Environment
 * 
 * Author: GR @Microsoft
 * Dated: 07-17-2020
 *
 * NOTES: Capture updates to the code below.
 */
namespace core.Constants
{
    public static class BatchVmConfig
    {
	public const string VM_IMAGE_PUBLISHER = "canonical";
	public const string VM_IMAGE_OFFER = "ubuntuserver";
	public const string VM_IMAGE_SKU = "18.04-lts";
    }
}
