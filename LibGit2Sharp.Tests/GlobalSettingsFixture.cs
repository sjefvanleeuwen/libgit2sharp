﻿using System;
using System.IO;
using System.Text.RegularExpressions;
using LibGit2Sharp.Core;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class GlobalSettingsFixture : BaseFixture
    {
        [Fact]
        public void CanGetMinimumCompiledInFeatures()
        {
            BuiltInFeatures features = GlobalSettings.Version.Features;

            Assert.True(features.HasFlag(BuiltInFeatures.Threads));
            Assert.True(features.HasFlag(BuiltInFeatures.Https));
        }

        [Fact]
        public void CanRetrieveValidVersionString()
        {
            // Version string format is:
            //      Major.Minor.Patch[-previewTag]+g{LibGit2Sharp_abbrev_hash}.libgit2-{libgit2_abbrev_hash} (x86|x64 - features)
            // Example output:
            //      "0.25.0-preview.52+g871d13a67f.libgit2-15e1193 (x86 - Threads, Https)"

            string versionInfo = GlobalSettings.Version.ToString();

            // The GlobalSettings.Version returned string should contain :
            //      version: '0.25.0[-previewTag]' LibGit2Sharp version number.
            //      git2SharpHash: '871d13a67f' LibGit2Sharp hash.
            //      arch: 'x86' or 'x64' libgit2 target.
            //      git2Features: 'Threads, Ssh' libgit2 features compiled with.
            string regex = @"^(?<version>\d+\.\d+\.\d+(-[\w\-\.]+)?\+(g(?<git2SharpHash>[a-f0-9]{10})\.)?libgit2-[a-f0-9]{7}) \((?<arch>\w+) - (?<git2Features>(?:\w*(?:, )*\w+)*)\)$";

            Assert.NotNull(versionInfo);

            Match regexResult = Regex.Match(versionInfo, regex);

            Assert.True(regexResult.Success, "The following version string format is enforced:" +
                                             "Major.Minor.Patch[-previewTag]+g{LibGit2Sharp_abbrev_hash}.libgit2-{libgit2_abbrev_hash} (x86|x64 - features). " +
                                             "But found \"" + versionInfo + "\" instead.");
        }

        [Fact]
        public void TryingToResetNativeLibraryPathAfterLoadedThrows()
        {
            // Do something that loads the native library
            var features = GlobalSettings.Version.Features;

            Assert.Throws<LibGit2SharpException>(() => { GlobalSettings.NativeLibraryPath = "C:/Foo"; });
        }

        [ConditionalFact(typeof(NetFramework))]
        public void LoadFromSpecifiedPath()
        {
#if NET461
            var nativeDllFileName = NativeDllName.Name + ".dll";

            var testAppExe = typeof(TestApp).Assembly.Location;
            var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var platformDir = Path.Combine(tempDir, "plat");

            try
            {
                Directory.CreateDirectory(Path.Combine(platformDir, "x86"));
                Directory.CreateDirectory(Path.Combine(platformDir, "x64"));

                File.Copy(Path.Combine(GlobalSettings.NativeLibraryPath, "x86", nativeDllFileName), Path.Combine(platformDir, "x86", nativeDllFileName));
                File.Copy(Path.Combine(GlobalSettings.NativeLibraryPath, "x64", nativeDllFileName), Path.Combine(platformDir, "x64", nativeDllFileName));

                var (output, exitCode) = ProcessHelper.RunProcess(testAppExe, arguments: $@"{NativeDllName.Name} ""{platformDir}""", workingDirectory: tempDir);

                Assert.Empty(output);
                Assert.Equal(0, exitCode);
            }
            finally
            {
                DirectoryHelper.DeleteDirectory(tempDir);
            }
#endif
        }
    }
}
