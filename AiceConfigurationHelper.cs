using aice_stable.services;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace aice_stable
{
    /// <summary>
    /// Class used to load configuration file and services
    /// </summary>
    public sealed class AiceConfigurationHelper
    {
        /// <summary>
        /// Empty constructor
        /// </summary>
        public AiceConfigurationHelper() { }

        /// <summary>
        /// Loads and process the configuration data
        /// </summary>
        /// <param name="file">Location of the file</param>
        /// <returns></returns>
        public async Task<AiceConfigurationData> LoadConfigurationAsync(FileInfo file)
        {
            /// Validates the file object
            if (file == null || !file.Exists)
                throw new ArgumentException("Specified file is not valid or does not exist.", nameof(file));

            // Loads the JSON data
            var json = "{}";
            using (FileStream fileStream = file.OpenRead())
            using (StreamReader streamReader = new StreamReader(fileStream, AiceUtilities.UTF8))
                json = await streamReader.ReadToEndAsync();

            /// Deserialize the configuration data using model class in AiceConfigurationData
            return JsonConvert.DeserializeObject<AiceConfigurationData>(json);
        }
    }
}
