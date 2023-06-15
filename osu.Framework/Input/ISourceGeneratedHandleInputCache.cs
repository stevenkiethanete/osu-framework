// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Input
{
    public interface ISourceGeneratedHandleInputCache
    {
        protected internal Type KnownType => typeof(object);
        protected internal bool RequestsPositionalInput => false;
        protected internal bool RequestsNonPositionalInput => false;
    }
}
