﻿using System;
using Cake.Core;
using Cake.Core.Annotations;
using Cake.Core.IO;
using Cake.Git.Extensions;
using LibGit2Sharp;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
namespace Cake.Git
{
    // ReSharper disable once PublicMembersMustHaveComments
    public static partial class GitAliases
    {
        /// <summary>
        /// Applys tagName to repository.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="repositoryDirectoryPath">Path to repository.</param>
        /// <param name="tagName">The tag name.</param>
        /// <exception cref="ArgumentNullException"></exception>
        [CakeMethodAlias]
        [CakeAliasCategory("Tag")]
        public static void GitTag(
            this ICakeContext context,
            DirectoryPath repositoryDirectoryPath,
            string tagName
            )
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (repositoryDirectoryPath == null)
            {
                throw new ArgumentNullException(nameof(repositoryDirectoryPath));
            }

            if (tagName == null)
            {
                throw new ArgumentNullException(nameof(tagName));
            }

            context.UseRepository(
                repositoryDirectoryPath,
                repository =>repository.ApplyTag(tagName)
                );
        }

        /// <summary>
        /// Applys tagName to repository.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="repositoryDirectoryPath">Path to repository.</param>
        /// <param name="tagName">The tag name.</param>
        /// <param name="objectish">The revparse spec for the target object.</param>
        /// <exception cref="ArgumentNullException"></exception>
        [CakeMethodAlias]
        [CakeAliasCategory("Tag")]
        public static void GitTag(
            this ICakeContext context,
            DirectoryPath repositoryDirectoryPath,
            string tagName,
            string objectish
            )
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (repositoryDirectoryPath == null)
            {
                throw new ArgumentNullException(nameof(repositoryDirectoryPath));
            }

            if (tagName == null)
            {
                throw new ArgumentNullException(nameof(tagName));
            }

            if (objectish == null)
            {
                throw new ArgumentNullException(nameof(objectish));
            }

            context.UseRepository(
                repositoryDirectoryPath,
                repository => repository.ApplyTag(tagName, objectish)
                );
        }
    }
}