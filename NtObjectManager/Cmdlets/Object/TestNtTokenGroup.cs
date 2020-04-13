﻿//  Copyright 2020 Google Inc. All Rights Reserved.
//
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//
//  http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using NtApiDotNet;
using System.Collections.Generic;
using System.Management.Automation;

namespace NtObjectManager.Cmdlets.Object
{
    /// <summary>
    /// <para type="synopsis">Checks if a SID is present in the Token's groups.</para>
    /// <para type="description">This cmdlet checks if a SID is present in a Token's groups. It supports checking
    /// for normal Groups, Restricted SIDs or Capabilites.</para>
    /// </summary>
    /// <example>
    ///   <code>Test-NtTokenGroup -Token $token -Sid $sid</code>
    ///   <para>Checks if SID is present in the normal Groups.</para>
    /// </example>
    /// <example>
    ///   <code>Test-NtTokenGroup -Token $token -Sid $sid -DenyOnly</code>
    ///   <para>Checks if SID is present in the normal Groups including DenyOnly groups.</para>
    /// </example>
    /// <example>
    ///   <code>Test-NtTokenGroup -Token $token -Sid $sid -Restricted</code>
    ///   <para>Checks if SID is present in the normal Groups.</para>
    /// </example>
    /// <example>
    ///   <code>Test-NtTokenGroup -Token $token -Sid $sid -Capability</code>
    ///   <para>Checks if SID is present in the normal Groups.</para>
    /// </example>
    [Cmdlet(VerbsDiagnostic.Test, "NtTokenGroup", DefaultParameterSetName = "FromGroup")]
    public class TestNtTokenGroup : PSCmdlet
    {
        /// <summary>
        /// <para type="description">Specify the token to test.</para>
        /// </summary>
        [Parameter(Position = 1)]
        public NtToken Token { get; set; }

        /// <summary>
        /// <para type="description">Specify the SID to test.</para>
        /// </summary>
        [Parameter(Position = 0, Mandatory = true)]
        public Sid Sid { get; set; }

        /// <summary>
        /// <para type="description">Specify the to test the Restricted SIDs.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "FromRestricted")]
        public SwitchParameter Restricted { get; set; }

        /// <summary>
        /// <para type="description">Specify the to test the Capability SIDs.</para>
        /// </summary>
        [Parameter(Mandatory = true, ParameterSetName = "FromCapability")]
        public SwitchParameter Capability { get; set; }

        /// <summary>
        /// <para type="description">Specify to also check DenyOnly SIDs.</para>
        /// </summary>
        [Parameter(ParameterSetName = "FromGroup")]
        public SwitchParameter DenyOnly { get; set; }

        /// <summary>
        /// Process record.
        /// </summary>
        protected override void ProcessRecord()
        {
            WriteObject(CheckGroups(Sid, GetGroups(), DenyOnly, Restricted));
        }

        private NtToken GetToken()
        {
            return Token?.Duplicate() ?? NtToken.OpenEffectiveToken();
        }

        private IEnumerable<UserGroup> GetGroups()
        {
            using (var token = GetToken())
            {
                if (Restricted)
                {
                    return token.RestrictedSids;
                }
                else if (Capability)
                {
                    return token.Capabilities;
                }

                List<UserGroup> groups = new List<UserGroup>(token.Groups);
                UserGroup user = token.User;
                if (!user.DenyOnly)
                {
                    user = new UserGroup(user.Sid, GroupAttributes.Enabled);
                }
                groups.Insert(0, user);

                return groups;
            }
        }

        private static bool CheckGroups(Sid sid, IEnumerable<UserGroup> groups, bool deny_only, bool restricted)
        {
            foreach (var group in groups)
            {
                if (group.Sid != sid)
                    continue;

                if (restricted || group.Enabled)
                    return true;
                if (deny_only && group.DenyOnly)
                    return true;
            }
            return false;
        }
    }
}
