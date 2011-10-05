<<<<<<< HEAD
﻿using NUnit.Framework;
using OpenWrap;
=======
﻿using System.Linq;
using NUnit.Framework;
using OpenWrap;
using OpenWrap.PackageModel;
using OpenWrap.Testing;
>>>>>>> Fixes issue with unrelated packages being updated
using Tests.Commands.update_wrap.project;

namespace Tests.Commands.add_wrap
{
    public class adding_can_downgrade_package : contexts.add_wrap
    {
        public adding_can_downgrade_package()
        {
            given_file_based_project_repository();

            given_project_package("sauron", "1.0.0", "depends: one-ring");
            given_project_package("one-ring", "2.0.0");

            given_dependency("depends: sauron");

            given_remote_package("frodo", "1.0.0".ToVersion(), "depends: one-ring = 1.0");
            given_remote_package("one-ring", "1.0.1".ToVersion());
            when_executing_command("frodo");
        }

        [Test]
        public void unrelated_dependency_is_not_updated()
        {
<<<<<<< HEAD
            Environment.ProjectRepository.ShouldHavePackage("one-ring", "1.0.1");
=======
            Environment.ProjectRepository.ShouldHavePackage("one-ring","1.0.1");
>>>>>>> Fixes issue with unrelated packages being updated
        }
    }
}