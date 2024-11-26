using System;
using System.Collections.Generic;

namespace LibGit2Sharp.Tests.TestHelpers
{
    public class TestRemoteRefs
    {
        /*
        * git ls-remote http://github.com/libgit2/TestGitRepository
        * 49322bb17d3acc9146f98c97d078513228bbf3c0        HEAD
        * 0966a434eb1a025db6b71485ab63a3bfbea520b6        refs/heads/first-merge
        * 49322bb17d3acc9146f98c97d078513228bbf3c0        refs/heads/master
        * 42e4e7c5e507e113ebbb7801b16b52cf867b7ce1        refs/heads/no-parent
        * d96c4e80345534eccee5ac7b07fc7603b56124cb        refs/tags/annotated_tag
        * c070ad8c08840c8116da865b2d65593a6bb9cd2a        refs/tags/annotated_tag^{}
        * 55a1a760df4b86a02094a904dfa511deb5655905        refs/tags/blob
        * 8f50ba15d49353813cc6e20298002c0d17b0a9ee        refs/tags/commit_tree
        * 6e0c7bdb9b4ed93212491ee778ca1c65047cab4e        refs/tags/nearly-dangling
        */
        /// <summary>
        /// Expected references on http://github.com/libgit2/TestGitRepository
        /// </summary>
        public static List<Tuple<string, string, bool>> ExpectedRemoteRefs = new List<Tuple<string, string, bool>>()
        {
            new Tuple<string, string, bool>("HEAD", "49322bb17d3acc9146f98c97d078513228bbf3c0", false),
            new Tuple<string, string, bool>("refs/heads/first-merge", "0966a434eb1a025db6b71485ab63a3bfbea520b6", false),
            new Tuple<string, string, bool>("refs/heads/master", "49322bb17d3acc9146f98c97d078513228bbf3c0", true),
            new Tuple<string, string, bool>("refs/heads/no-parent", "42e4e7c5e507e113ebbb7801b16b52cf867b7ce1", false),
            new Tuple<string, string, bool>("refs/tags/annotated_tag", "d96c4e80345534eccee5ac7b07fc7603b56124cb", false),
            new Tuple<string, string, bool>("refs/tags/annotated_tag^{}", "c070ad8c08840c8116da865b2d65593a6bb9cd2a", false),
            new Tuple<string, string, bool>("refs/tags/blob", "55a1a760df4b86a02094a904dfa511deb5655905", false),
            new Tuple<string, string, bool>("refs/tags/commit_tree", "8f50ba15d49353813cc6e20298002c0d17b0a9ee", false),
            new Tuple<string, string, bool>("refs/tags/nearly-dangling", "6e0c7bdb9b4ed93212491ee778ca1c65047cab4e", false),
        };
    }
}
