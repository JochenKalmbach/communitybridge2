﻿using System.Resources;

namespace CommunityBridge2.LiveConnect.Internal
{
  internal static class ResourceHelper
    {
        private static readonly ResourceManager resourceManager;

        static ResourceHelper()
        {
            resourceManager = new ResourceManager(typeof(Resources));
        }

        public static string GetString(string name)
        {
            return resourceManager.GetString(name);
        }
    }
}
