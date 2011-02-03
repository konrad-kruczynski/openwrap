﻿using System;
using System.Linq;
using NUnit.Framework;
using OpenWrap.Commands.contexts;
using OpenWrap.Commands.Wrap;
using OpenWrap.Testing;
using OpenWrap.Tests.Commands.context;

namespace OpenWrap.Commands.add_wrap
{
    class adding_wrap_from_local_package_outside_of_project_path : command_context<AddWrapCommand>
    {
        public adding_wrap_from_local_package_outside_of_project_path()
        {
            given_currentdirectory_package("sauron", new Version(1, 0, 0));

            when_executing_command("-Name", "sauron", "-System");
        }
        [Test]
        public void installs_package_in_system_repository()
        {
            Environment.SystemRepository.PackagesByName["sauron"]
                    .ShouldHaveCountOf(1)
                    .First().Version.ShouldBe(new Version(1, 0, 0));
        }
        [Test]
        public void command_is_successful()
        {
            Results.ShouldHaveAll(x => x.Success());
        }
    }
}