using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using NLog;

namespace VersionOne.Integration.HelpStar
{
    class Program
    {
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        private static IntegrationConfiguration _config;
        private static VersionOneClient _v1Client;
        private static HelpStarClient _hsClient;

        static void Main(string[] args)
        {
            try
            {
                //Start logging.
                CreateLogHeader();

                //Initialize config file.
                _logger.Info("Initializing configurations.");
                _config = (IntegrationConfiguration)ConfigurationManager.GetSection("integration");

                //Verify configurations.
                _logger.Info("Verifying configurations.");
                VerifyV1Configuration();


                //**** Get HelpStar tickets code goes here. Perhaps calling a stored procedure in the HS DB? ****

                //**** Process HelpStar tickets code goes here. Should loop through the results and process accordingly. ****


                _logger.Info("Integration processing complete.");
            }
            catch (Exception ex)
            {
                _logger.Error("ERROR: " + ex.Message);
                _logger.Info("Integration processing terminated.");
            }
            finally 
            {
                _logger.Info(String.Empty);
                _logger.Info(String.Empty);

                //DEBUG ONLY:
                Console.Write("\nPress ENTER to close... ");
                Console.ReadLine();
            }
            Environment.Exit(0);
        }

        private static void CreateLogHeader()
        {
            _logger.Info("*********************************************************");
            _logger.Info("* VersionOne HelpStar Integration Log");
            _logger.Info("* {0}", DateTime.Now);
            _logger.Info("*********************************************************");
            _logger.Info("");
        }

        //Verifies the connection to the V1 instance. Makes use of the ConnectAttempts configuration to control how many times to attempt a connection.
        private static void VerifyV1Configuration()
        {
            try
            {
                _logger.Info("Attempting to connect to V1.");
                _v1Client = new VersionOneClient(_config, _logger);

                for (int i = 1; i < _config.V1Connection.ConnectAttempts + 1; i++)
                {
                    _logger.Info("Connection attempt {0}.", i.ToString());
                    try
                    {
                        if (_v1Client.CheckAuthentication() == true) break;
                    }
                    catch (Exception ex)
                    {
                        if (i < _config.V1Connection.ConnectAttempts)
                        {
                            System.Threading.Thread.Sleep(5000);
                            continue;
                        }
                        else
                            throw ex;
                    }
                }
                string version = _v1Client.GetV1Version();
                _logger.Info("V1 connection verified.");
                _logger.Debug("-> Build: " + version);
            }
            catch (Exception ex)
            {
                _logger.Debug("-> Unable to connect to V1.");
                throw ex;
            }
        }
    }
}
