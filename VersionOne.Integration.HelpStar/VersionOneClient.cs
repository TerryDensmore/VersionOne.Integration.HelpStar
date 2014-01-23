using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NLog;
using VersionOne.SDK.APIClient;

namespace VersionOne.Integration.HelpStar
{
    internal class VersionOneClient
    {
        private readonly Logger _logger;
        private readonly IntegrationConfiguration _config;

        //V1 variables.
        private readonly string _V1_META_URL;
        private readonly string _V1_DATA_URL;
        private MetaModel _v1Meta;
        private Services _v1Data;

        public enum V1AssetType
        {
            Defect,
            Story
        }

        //Constructor: Initializes the V1 instance connection.
        public VersionOneClient(IntegrationConfiguration Config, Logger Logger)
        {
            _logger = Logger;
            _config = Config;
            _V1_META_URL = Config.V1Connection.Url + "/meta.v1/";
            _V1_DATA_URL = Config.V1Connection.Url + "/rest-1.v1/";
            V1APIConnector metaConnector = new V1APIConnector(_V1_META_URL);
            V1APIConnector dataConnector;

            //Set V1 connection based on authentication type.
            if (_config.V1Connection.UseWindowsAuthentication == true)
            {
                _logger.Debug("-> Connecting with Windows Authentication.");

                //If username is not specified, try to connect using domain credentials.
                if (String.IsNullOrEmpty(_config.V1Connection.Username))
                {
                    _logger.Debug("-> Connecting with default Windows domain account: {0}\\{1}", Environment.UserDomainName, Environment.UserName);
                    dataConnector = new V1APIConnector(_V1_DATA_URL);
                }
                else
                {
                    _logger.Debug("-> Connecting with specified Windows domain account: {0}", _config.V1Connection.Username);
                    dataConnector = new V1APIConnector(_V1_DATA_URL, _config.V1Connection.Username, _config.V1Connection.Password, true);
                }
            }
            else
            {
                _logger.Debug("-> Connecting with V1 account credentials: {0}", _config.V1Connection.Username);
                dataConnector = new V1APIConnector(_V1_DATA_URL, _config.V1Connection.Username, _config.V1Connection.Password);
            }

            _v1Meta = new MetaModel(metaConnector);
            _v1Data = new Services(_v1Meta, dataConnector);
        }

        //Get the V1 version number.
        public string GetV1Version()
        {
            try
            {
                Version ver = _v1Meta.Version;
                return ver.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("Error connecting to VersionOne. " + ex.Message);
            }
        }

        ////Save defect in V1.
        //public void SaveDefect(IntegrationConfiguration.ProjectInfo project, Defect defect, V1AssetType V1Type)
        //{
        //    try
        //    {
        //        Dictionary<string, IntegrationConfiguration.FieldInfo> fields = project.Defects.FieldMappings;
        //        bool releaseFound = false;
        //        bool ownerFound = false;

        //        //ASSET TYPE: Create the defect as a V1 story or defect.
        //        IAssetType assetType;
        //        if (V1Type == V1AssetType.Story)
        //        {
        //            _logger.Trace("-> Saving asset type: Story");
        //            assetType = _v1Meta.GetAssetType("Story");
        //        }
        //        else
        //        {
        //            _logger.Trace("-> Saving asset type: Defect");
        //            assetType = _v1Meta.GetAssetType("Defect");
        //        }
        //        Asset asset = _v1Data.New(assetType, null);

        //        //TITLE (Summary): Required. No check for enabled status.
        //        if (!String.IsNullOrEmpty(defect.Title))
        //        {
        //            _logger.Trace("-> Saving Title: {0}", defect.Title);
        //            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition(fields["Summary"].V1FieldName);
        //            asset.SetAttributeValue(nameAttribute, defect.Title);
        //        }
        //        else
        //        {
        //            throw new Exception("Title is a required field in V1.");
        //        }

        //        //DESCRIPTION: Optional.
        //        if (fields["Description"].Enabled == true & fields["Description"].CreateV1Enabled == true & !String.IsNullOrEmpty(defect.Description))
        //        {
        //            _logger.Trace("-> Saving Description: {0}", defect.Description);
        //            IAttributeDefinition descAttribute = assetType.GetAttributeDefinition(fields["Description"].V1FieldName);
        //            asset.SetAttributeValue(descAttribute, defect.Description);
        //        }

        //        //SCOPE (Project): Optional. If the "TargetInRelease" is enabled and specified, check that it exists in V1. If not, place defect in main project backlog.
        //        IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition(fields["TargetInRelease"].V1FieldName);
        //        if (fields["TargetInRelease"].Enabled == true & fields["TargetInRelease"].CreateV1Enabled == true & !String.IsNullOrEmpty(defect.ALMTargetInRelease))
        //        {
        //            _logger.Trace("-> Checking for TargetInRelease: {0}", defect.ALMTargetInRelease);
        //            string releaseOID = CheckForExistingRelease(defect.Project, GetV1ReleaseName(project, defect.ALMTargetInRelease));
        //            if (!String.IsNullOrEmpty(releaseOID))
        //            {
        //                _logger.Trace("-> Saving TargetInRelease: {0} as V1 OID: {1}", defect.ALMTargetInRelease, releaseOID);
        //                asset.SetAttributeValue(scopeAttribute, releaseOID);
        //                releaseFound = true;
        //            }
        //            else
        //            {
        //                _logger.Trace("-> Saving TargetInRelease: {0} as V1 OID: {1}", defect.ALMTargetInRelease, defect.Project);
        //                asset.SetAttributeValue(scopeAttribute, GetAssetIDFromName("Scope", defect.Project));
        //            }
        //        }
        //        else
        //        {
        //            _logger.Trace("-> Saving TargetInRelease: {0} as V1 OID: {1}", defect.ALMTargetInRelease, defect.Project);
        //            asset.SetAttributeValue(scopeAttribute, GetAssetIDFromName("Scope", defect.Project));
        //            releaseFound = true;
        //        }

        //        //SOURCE: Required. This value is hard coded when the defect is read from ALM. Always = "ALM".
        //        _logger.Trace("-> Saving Source: {0}", defect.Source);
        //        IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source");
        //        asset.SetAttributeValue(sourceAttribute, GetAssetIDFromName("StorySource", defect.Source));

        //        //REFERENCE: Required. This value is hard coded when the defect is read from ALM.
        //        _logger.Trace("-> Saving Reference: {0}", defect.Reference);
        //        IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition(fields["DefectID"].V1FieldName);
        //        asset.SetAttributeValue(referenceAttribute, defect.Reference);

        //        //OWNER: Optional. Check that owner is valid member in V1.
        //        if (fields["AssignedTo"].Enabled == true & fields["AssignedTo"].CreateV1Enabled == true & !String.IsNullOrEmpty(defect.Owner))
        //        {
        //            //Set the domain\username if using windows authentication.
        //            string owner = String.Empty;
        //            if (_config.V1Connection.UseWindowsAuthentication == true)
        //            {
        //                owner = project.V1Domain + "\\" + defect.Owner;
        //            }
        //            else
        //            {
        //                owner = defect.Owner;
        //            }

        //            _logger.Trace("-> Saving Owner: {0}", owner);
        //            if (CheckForExistingMember(owner) == true)
        //            {
        //                IAttributeDefinition ownerAttribute = assetType.GetAttributeDefinition(fields["AssignedTo"].V1FieldName);
        //                asset.AddAttributeValue(ownerAttribute, GetAssetIDFromUsername("Member", owner));
        //                ownerFound = true;
        //            }
        //        }

        //        //PRIORITY: Optional.
        //        if (fields["Priority"].Enabled == true & fields["Priority"].CreateV1Enabled == true & !String.IsNullOrEmpty(defect.Priority))
        //        {
        //            _logger.Trace("-> Saving Priority: {0}", defect.Priority);
        //            IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition(fields["Priority"].V1FieldName);
        //            asset.SetAttributeValue(priorityAttribute, GetAssetIDFromName("WorkitemPriority", defect.Priority));
        //        }

        //        //SEVERITY: Optional.
        //        if (fields["Severity"].Enabled == true & fields["Severity"].CreateV1Enabled == true & !String.IsNullOrEmpty(defect.Severity))
        //        {
        //            _logger.Trace("-> Saving Severity: {0}", defect.Severity);
        //            IAttributeDefinition severityAttribute = assetType.GetAttributeDefinition(fields["Severity"].V1FieldName);
        //            asset.SetAttributeValue(severityAttribute, GetAssetIDFromName(fields["Severity"].V1FieldName, defect.Severity));
        //        }

        //        //STATUS: Optional.
        //        if (fields["Status"].Enabled == true & fields["Status"].CreateV1Enabled == true & !String.IsNullOrEmpty(defect.Status))
        //        {
        //            _logger.Trace("-> Saving Status: {0}", defect.Status);
        //            IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition(fields["Status"].V1FieldName);
        //            asset.SetAttributeValue(statusAttribute, GetAssetIDFromName("StoryStatus", defect.Status));
        //        }

        //        //FOUND BY (Detected By): Optional. Only V1 defect asset type has "found by" field.
        //        if (fields["DetectedBy"].Enabled == true & fields["DetectedBy"].CreateV1Enabled == true & !String.IsNullOrEmpty(defect.FoundBy) & asset.AssetType.Token == "Defect")
        //        {
        //            _logger.Trace("-> Saving FoundBy: {0}", defect.FoundBy);
        //            IAttributeDefinition foundByAttribute = assetType.GetAttributeDefinition(fields["DetectedBy"].V1FieldName);
        //            asset.SetAttributeValue(foundByAttribute, defect.FoundBy);
        //        }

        //        //TEAM: Optional.
        //        if (fields["Team"].Enabled == true & fields["Team"].CreateV1Enabled == true & !String.IsNullOrEmpty(defect.Team))
        //        {
        //            _logger.Trace("-> Saving Team: {0}", defect.Team);
        //            IAttributeDefinition teamAttribute = assetType.GetAttributeDefinition(fields["Team"].V1FieldName);
        //            asset.SetAttributeValue(teamAttribute, GetAssetIDFromName(fields["Team"].V1FieldName, defect.Team));
        //        }

        //        //Save the new defect to V1.
        //        _v1Data.Save(asset);

        //        //Update the new defect values for saving to ALM: OID, Number, URL, and Comment for the new defect.
        //        defect.ID = asset.Oid.Momentless.ToString();
        //        defect.Number = GetAssetNumber(asset);
        //        defect.URL = _config.V1Connection.Url + "/defect.mvc/Summary?oidToken=" + defect.ID;

        //        //Comments enabled in config file.
        //        if (project.Defects.V1CommentsEnabled == true)
        //        {
        //            defect.ALMComment = ALMComments.CreatedDefectComment(defect);
        //        }

        //        //Log it.
        //        _logger.Info("Created defect in V1: {0}", defect.ToString());
        //        _logger.Debug("-> URL: " + defect.URL);
        //        if (releaseFound == false) _logger.Debug("-> Release \"{0}\" is not a valid project in V1. Defect was created in the \"{1}\" project.", defect.ALMTargetInRelease, defect.Project);
        //        if (ownerFound == false) _logger.Debug("-> Owner \"{0}\" is not a valid member in V1. Defect will be created as unassigned.", defect.Owner);

        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error("Error saving defect {0} in V1.", defect.ToString());
        //        _logger.Error("ERROR: " + ex.Message);
        //    }
        //}

        ////Get the V1 release name from release mappings.
        //private string GetV1ReleaseName(IntegrationConfiguration.ProjectInfo project, string ALMReleaseName)
        //{
        //    string releaseName = String.Empty;
        //    foreach (IntegrationConfiguration.ReleaseMapping release in project.Releases.ReleaseMappings)
        //    {
        //        if (release.ALMName == ALMReleaseName)
        //        {
        //            releaseName = release.V1Name;
        //            break;
        //        }
        //    }
        //    return releaseName;
        //}

        ////Update asset in V1.
        //public void UpdateDefect(IntegrationConfiguration.ProjectInfo project, Defect defect)
        //{
        //    try
        //    {
        //        Dictionary<string, IntegrationConfiguration.FieldInfo> fields = project.Defects.FieldMappings;
        //        bool releaseFound = false;
        //        bool ownerFound = false;

        //        //Get the asset to update.
        //        _logger.Trace("-> Updating V1 asset: {0}", defect.ALMVersionOneID);
        //        Asset asset = GetAssetByNumber(defect.ALMVersionOneID);
        //        IAssetType assetType = asset.AssetType;

        //        //TITLE (Summary): Required.
        //        if (fields["Summary"].UpdateV1Enabled == true & !String.IsNullOrEmpty(defect.Title))
        //        {
        //            _logger.Trace("-> Updating Title: {0}", defect.Title);
        //            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition(fields["Summary"].V1FieldName);
        //            asset.SetAttributeValue(nameAttribute, defect.Title);
        //        }

        //        //DESCRIPTION: Optional.
        //        if (fields["Description"].Enabled == true & fields["Description"].UpdateV1Enabled == true & !String.IsNullOrEmpty(defect.Description))
        //        {
        //            _logger.Trace("-> Updating Description: {0}", defect.Description);
        //            IAttributeDefinition descAttribute = assetType.GetAttributeDefinition(fields["Description"].V1FieldName);
        //            asset.SetAttributeValue(descAttribute, defect.Description);
        //        }

        //        //SCOPE (Project): Optional.
        //        IAttributeDefinition scopeAttribute = assetType.GetAttributeDefinition(fields["TargetInRelease"].V1FieldName);
        //        if (fields["TargetInRelease"].Enabled == true & fields["TargetInRelease"].UpdateV1Enabled == true & !String.IsNullOrEmpty(defect.ALMTargetInRelease))
        //        {
        //            _logger.Trace("-> Checking for TargetInRelease: {0}", defect.ALMTargetInRelease);
        //            string releaseOID = CheckForExistingRelease(defect.Project, GetV1ReleaseName(project, defect.ALMTargetInRelease));
        //            if (!String.IsNullOrEmpty(releaseOID))
        //            {
        //                _logger.Trace("-> Updating TargetInRelease: {0} as V1 OID: {1}", defect.ALMTargetInRelease, releaseOID);
        //                asset.SetAttributeValue(scopeAttribute, releaseOID);
        //                releaseFound = true;
        //            }
        //            else
        //            {
        //                _logger.Trace("-> Updating TargetInRelease: {0} as V1 OID: {1}", defect.ALMTargetInRelease, defect.Project);
        //                asset.SetAttributeValue(scopeAttribute, GetAssetIDFromName("Scope", defect.Project));
        //            }
        //        }
        //        else
        //        {
        //            _logger.Trace("-> Updating TargetInRelease: {0} as V1 OID: {1}", defect.ALMTargetInRelease, defect.Project);
        //            asset.SetAttributeValue(scopeAttribute, GetAssetIDFromName("Scope", defect.Project));
        //            releaseFound = true;
        //        }

        //        //OWNER: Optional.
        //        if (fields["AssignedTo"].Enabled == true & fields["AssignedTo"].UpdateV1Enabled == true & !String.IsNullOrEmpty(defect.Owner))
        //        {
        //            //Set the domain\username if using windows authentication.
        //            string owner = String.Empty;
        //            if (_config.V1Connection.UseWindowsAuthentication == true)
        //            {
        //                owner = project.V1Domain + "\\" + defect.Owner;
        //            }
        //            else
        //            {
        //                owner = defect.Owner;
        //            }

        //            _logger.Trace("-> Updating Owner: {0}", owner);
        //            if (CheckForExistingMember(owner) == true)
        //            {
        //                //Replace all exisiting owners with owner from ALM.
        //                ArrayList ownersList = GetAssetOwners(asset);
        //                IAttributeDefinition ownerAttribute = assetType.GetAttributeDefinition(fields["AssignedTo"].V1FieldName);
        //                foreach (string ownerToRemove in ownersList)
        //                {
        //                    asset.RemoveAttributeValue(ownerAttribute, GetAssetIDFromUsername("Member", ownerToRemove));
        //                }
        //                asset.AddAttributeValue(ownerAttribute, GetAssetIDFromUsername("Member", owner));
        //                ownerFound = true;
        //            }
        //        }

        //        //PRIORITY: Optional.
        //        if (fields["Priority"].Enabled == true & fields["Priority"].UpdateV1Enabled == true & !String.IsNullOrEmpty(defect.Priority))
        //        {
        //            _logger.Trace("-> Updating Priority: {0}", defect.Priority);
        //            IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition(fields["Priority"].V1FieldName);
        //            asset.SetAttributeValue(priorityAttribute, GetAssetIDFromName("WorkitemPriority", defect.Priority));
        //        }

        //        //SEVERITY: Optional.
        //        if (fields["Severity"].Enabled == true & fields["Severity"].UpdateV1Enabled == true & !String.IsNullOrEmpty(defect.Severity))
        //        {
        //            _logger.Trace("-> Updating Severity: {0}", defect.Severity);
        //            IAttributeDefinition severityAttribute = assetType.GetAttributeDefinition(fields["Severity"].V1FieldName);
        //            asset.SetAttributeValue(severityAttribute, GetAssetIDFromName(fields["Severity"].V1FieldName, defect.Severity));
        //        }

        //        //STATUS: Optional.
        //        if (fields["Status"].Enabled == true & fields["Status"].UpdateV1Enabled == true & !String.IsNullOrEmpty(defect.Status))
        //        {
        //            _logger.Trace("-> Updating Status: {0}", defect.Status);
        //            IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition(fields["Status"].V1FieldName);
        //            asset.SetAttributeValue(statusAttribute, GetAssetIDFromName("StoryStatus", defect.Status));
        //        }

        //        //TEAM: Optional.
        //        if (fields["Team"].Enabled == true & fields["Team"].UpdateV1Enabled == true & !String.IsNullOrEmpty(defect.Team))
        //        {
        //            _logger.Trace("-> Updating Team: {0}", defect.Team);
        //            IAttributeDefinition teamAttribute = assetType.GetAttributeDefinition(fields["Team"].V1FieldName);
        //            asset.SetAttributeValue(teamAttribute, GetAssetIDFromName(fields["Team"].V1FieldName, defect.Team));
        //        }

        //        //Save the updated asset.
        //        _v1Data.Save(asset);

        //        //Update the defect values for ALM: Populate OID, Number, URL, Comment, and Status for the new defect.
        //        defect.ID = asset.Oid.Momentless.ToString();
        //        defect.Number = GetAssetNumber(asset);
        //        defect.URL = _config.V1Connection.Url + "/defect.mvc/Summary?oidToken=" + defect.ID;

        //        //Comments enabled in config file.
        //        if (project.Defects.V1CommentsEnabled == true)
        //            defect.ALMComment = ALMComments.UpdatedDefectComment(defect);

        //        //Log it.
        //        _logger.Info("Updated asset in V1: {0}", defect.ALMVersionOneID);
        //        _logger.Debug("-> URL: " + defect.URL);
        //        if (releaseFound == false) _logger.Debug("-> Release \"{0}\" is not a valid project in V1. Defect was created in the \"{1}\" project.", defect.ALMTargetInRelease, defect.Project);
        //        if (ownerFound == false) _logger.Debug("-> Owner \"{0}\" is not a valid member in V1. Defect will be created as unassigned.", defect.Owner);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error("Error updating asset {0} in V1.", defect.ToString());
        //        _logger.Error("ERROR: " + ex.Message);
        //    }
        //}


        ////Close asset in V1.
        //public void CloseDefect(IntegrationConfiguration.ProjectInfo project, Defect defect)
        //{
        //    try
        //    {
        //        //Check if defect has already been closed.
        //        if (CheckForClosedAsset(defect.ALMVersionOneID) == true)
        //        {
        //            _logger.Info("Defect already closed in V1: {0}", defect.ALMVersionOneID);
        //        }
        //        else
        //        {
        //            Dictionary<string, IntegrationConfiguration.FieldInfo> fields = project.Defects.FieldMappings;
        //            Asset asset = GetAssetByNumber(defect.ALMVersionOneID);
        //            IAssetType assetType = asset.AssetType;

        //            //Set the statis value first.
        //            if (fields["Status"].Enabled == true & fields["Status"].UpdateV1Enabled == true & !String.IsNullOrEmpty(defect.Status))
        //            {
        //                IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition(fields["Status"].V1FieldName);
        //                asset.SetAttributeValue(statusAttribute, GetAssetIDFromName("StoryStatus", defect.Status));
        //            }
        //            _v1Data.Save(asset);

        //            //Update the defect values for ALM: Populate OID, Number, URL, Comment, and Status for the new defect.
        //            defect.ID = asset.Oid.Momentless.ToString();
        //            defect.Number = GetAssetNumber(asset);
        //            defect.URL = _config.V1Connection.Url + "/defect.mvc/Summary?oidToken=" + defect.ID;

        //            //Now close it.
        //            IOperation operation = _v1Meta.GetOperation("Defect.Inactivate");
        //            Oid oid = _v1Data.ExecuteOperation(operation, asset.Oid);

        //            //Log it.
        //            _logger.Info("Closed V1 asset: {0}", defect.ALMVersionOneID);
        //            _logger.Debug("-> URL: " + defect.URL);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Error("Error closing asset {0} in V1.", defect.ToString());
        //        _logger.Error("ERROR: " + ex.Message);
        //    }
        //}

        ////Gets a single asset from V1 given the asset number (ex: B-10293/D-56474).
        //public Defect GetDefect(string AssetNumber)
        //{
        //    try
        //    {
        //        //Get V1 asset with multiple attributes.
        //        IAssetType assetType = _v1Meta.GetAssetType("PrimaryWorkitem");
        //        Query query = new Query(assetType);

        //        IAttributeDefinition numberAttribute = assetType.GetAttributeDefinition("Number");
        //        query.Selection.Add(numberAttribute);
        //        IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
        //        query.Selection.Add(nameAttribute);
        //        IAttributeDefinition descAttribute = assetType.GetAttributeDefinition("Description");
        //        query.Selection.Add(descAttribute);
        //        IAttributeDefinition projectAttribute = assetType.GetAttributeDefinition("Scope.Name");
        //        query.Selection.Add(projectAttribute);
        //        IAttributeDefinition sourceAttribute = assetType.GetAttributeDefinition("Source.Name");
        //        query.Selection.Add(sourceAttribute);
        //        IAttributeDefinition referenceAttribute = assetType.GetAttributeDefinition("Reference");
        //        query.Selection.Add(referenceAttribute);
        //        IAttributeDefinition ownerAttribute = assetType.GetAttributeDefinition("Owners.Username");
        //        query.Selection.Add(ownerAttribute);
        //        IAttributeDefinition priorityAttribute = assetType.GetAttributeDefinition("Priority.Name");
        //        query.Selection.Add(priorityAttribute);
        //        IAttributeDefinition severityAttribute = assetType.GetAttributeDefinition("Custom_Severity.Name");
        //        query.Selection.Add(severityAttribute);
        //        IAttributeDefinition statusAttribute = assetType.GetAttributeDefinition("Status.Name");
        //        query.Selection.Add(statusAttribute);
        //        IAttributeDefinition teamAttribute = assetType.GetAttributeDefinition("Team.Name");
        //        query.Selection.Add(teamAttribute);
        //        IAttributeDefinition createDateAttribute = assetType.GetAttributeDefinition("CreateDate");
        //        query.Selection.Add(createDateAttribute);
        //        IAttributeDefinition changeDateAttribute = assetType.GetAttributeDefinition("ChangeDate");
        //        query.Selection.Add(changeDateAttribute);

        //        //Set the query filter.
        //        FilterTerm term = new FilterTerm(numberAttribute);
        //        term.Equal(AssetNumber);
        //        query.Filter = term;
        //        QueryResult result = _v1Data.Retrieve(query);

        //        if (result.Assets.Count > 0)
        //        {
        //            Asset asset = result.Assets[0];
        //            Defect defect = new Defect();
        //            defect.ID = asset.Oid.Token;
        //            defect.Number = (string)asset.GetAttribute(numberAttribute).Value;
        //            defect.Title = (string)asset.GetAttribute(nameAttribute).Value;
        //            defect.Description = (string)asset.GetAttribute(descAttribute).Value;
        //            defect.Project = (string)asset.GetAttribute(projectAttribute).Value;
        //            defect.Release = (string)asset.GetAttribute(projectAttribute).Value;
        //            defect.Source = (string)asset.GetAttribute(sourceAttribute).Value;
        //            defect.Reference = (string)asset.GetAttribute(referenceAttribute).Value;

        //            if (asset.GetAttribute(ownerAttribute).ValuesList.Count > 0)
        //                defect.Owner = (string)asset.GetAttribute(ownerAttribute).ValuesList[0];

        //            defect.Priority = (string)asset.GetAttribute(priorityAttribute).Value;
        //            defect.Severity = (string)asset.GetAttribute(severityAttribute).Value;
        //            defect.Status = (string)asset.GetAttribute(statusAttribute).Value;
        //            defect.Team = (string)asset.GetAttribute(teamAttribute).Value;
        //            defect.URL = _config.V1Connection.Url + "/defect.mvc/Summary?oidToken=" + defect.ID;
        //            defect.CreateDate = asset.GetAttribute(createDateAttribute).Value.ToString();
        //            defect.ChangeDate = asset.GetAttribute(changeDateAttribute).Value.ToString();
        //            return defect;
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.Debug("Error getting asset \"{0}\" in V1.", AssetNumber);
        //        _logger.Error("ERROR: " + ex.Message);
        //        return null;
        //    }
        //}

        ////Get the internal V1 asset ID.
        //private string GetAssetIDFromName(string AssetType, string Name)
        //{
        //    IAssetType assetType = _v1Meta.GetAssetType(AssetType);
        //    Query query = new Query(assetType);
        //    IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Name");
        //    FilterTerm term = new FilterTerm(nameAttribute);
        //    term.Equal(Name);
        //    query.Filter = term;

        //    QueryResult result;
        //    try
        //    {
        //        result = _v1Data.Retrieve(query);
        //    }
        //    catch
        //    {
        //        return String.Empty;
        //    }

        //    if (result.TotalAvaliable > 0)
        //        return result.Assets[0].Oid.Token;
        //    else
        //        return String.Empty;
        //}

        ////Get the internal V1 asset ID.
        //private string GetAssetIDFromUsername(string AssetType, string Username)
        //{
        //    IAssetType assetType = _v1Meta.GetAssetType(AssetType);
        //    Query query = new Query(assetType);
        //    IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Username");
        //    FilterTerm term = new FilterTerm(nameAttribute);
        //    term.Equal(Username);
        //    query.Filter = term;

        //    QueryResult result;
        //    try
        //    {
        //        result = _v1Data.Retrieve(query);
        //    }
        //    catch
        //    {
        //        return String.Empty;
        //    }

        //    if (result.TotalAvaliable > 0)
        //        return result.Assets[0].Oid.Token;
        //    else
        //        return String.Empty;
        //}

        ////Get the V1 asset number given the V1 OID.
        //private string GetAssetNumber(Asset asset)
        //{
        //    Query query = new Query(asset.Oid.Momentless);
        //    IAttributeDefinition nameAttribute = asset.AssetType.GetAttributeDefinition("Number");
        //    query.Selection.Add(nameAttribute);
        //    return _v1Data.Retrieve(query).Assets[0].GetAttribute(nameAttribute).Value.ToString();
        //}

        ////Get the V1 asset owners given the V1 Asset OID.
        //private ArrayList GetAssetOwners(Asset asset)
        //{
        //    Query query = new Query(asset.Oid.Momentless);
        //    IAttributeDefinition nameAttribute = asset.AssetType.GetAttributeDefinition("Owners.Username");
        //    query.Selection.Add(nameAttribute);
        //    QueryResult result = _v1Data.Retrieve(query);
        //    return (ArrayList)result.Assets[0].GetAttribute(nameAttribute).Values;
        //}

        ////Get the V1 asset given the asset number.
        //private Asset GetAssetByNumber(string AssetNumber)
        //{
        //    IAssetType assetType = _v1Meta.GetAssetType("Workitem");
        //    Query query = new Query(assetType);
        //    IAttributeDefinition numberAttribute = assetType.GetAttributeDefinition("Number");
        //    FilterTerm term = new FilterTerm(numberAttribute);
        //    term.Equal(AssetNumber);
        //    query.Filter = term;
        //    QueryResult result = _v1Data.Retrieve(query);
        //    if (result.Assets.Count > 0)
        //        return result.Assets[0];
        //    else
        //    {
        //        throw new Exception(String.Format("V1 asset \"{0}\" not found.", AssetNumber));
        //    }
        //}

        ////Check that member exists in V1.
        //private bool CheckForExistingMember(string username)
        //{
        //    IAssetType assetType = _v1Meta.GetAssetType("Member");
        //    Query query = new Query(assetType);

        //    IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Username");
        //    query.Selection.Add(nameAttribute);

        //    FilterTerm nameFilter = new FilterTerm(nameAttribute);
        //    nameFilter.Equal(username);
        //    query.Filter = nameFilter;

        //    QueryResult result = _v1Data.Retrieve(query);
        //    return result.TotalAvaliable > 0 ? true : false;
        //}

        ////Check that release (project) exists in V1, return the OID if found.
        //private string CheckForExistingRelease(string projectName, string releaseName)
        //{
        //    IAssetType assetType = _v1Meta.GetAssetType("Scope");
        //    Query query = new Query(assetType);

        //    IAttributeDefinition projectNameAttribute = assetType.GetAttributeDefinition("Parent.Name");
        //    IAttributeDefinition releaseNameAttribute = assetType.GetAttributeDefinition("Name");
        //    query.Selection.Add(projectNameAttribute);

        //    FilterTerm parentFilter = new FilterTerm(projectNameAttribute);
        //    parentFilter.Equal(projectName);
        //    FilterTerm releaseFilter = new FilterTerm(releaseNameAttribute);
        //    releaseFilter.Equal(releaseName);

        //    FilterTerm[] filters = { parentFilter, releaseFilter };
        //    query.Filter = new AndFilterTerm(filters);

        //    QueryResult result = _v1Data.Retrieve(query);
        //    if (result.TotalAvaliable > 0)
        //    {
        //        return result.Assets[0].Oid.ToString();
        //    }
        //    else
        //    {
        //        return String.Empty;
        //    }
        //}

        ////Check that defect already exists in V1.
        //public bool CheckForExistingAsset(string AssetID)
        //{
        //    IAssetType assetType = _v1Meta.GetAssetType("Workitem");
        //    Query query = new Query(assetType);
        //    IAttributeDefinition numberAttribute = assetType.GetAttributeDefinition("Number");
        //    query.Selection.Add(numberAttribute);
        //    query.Find = new QueryFind(AssetID, new AttributeSelection(numberAttribute));
        //    QueryResult result = _v1Data.Retrieve(query);
        //    return result.TotalAvaliable > 0 ? true : false;
        //}

        public bool CheckAuthentication()
        {
            IAssetType assetType = _v1Meta.GetAssetType("Member");
            Query query = new Query(assetType);
            IAttributeDefinition nameAttribute = assetType.GetAttributeDefinition("Username");
            query.Selection.Add(nameAttribute);
            FilterTerm idFilter = new FilterTerm(nameAttribute);
            idFilter.Equal(_config.V1Connection.Username);
            query.Filter = idFilter;
            QueryResult result = _v1Data.Retrieve(query);
            if (result.TotalAvaliable > 0)
            {
                return true;
            }
            else
            {
                throw new Exception(String.Format("Unable to validate connection to {0} with username {1}. You may not have permission to access this instance.", _config.V1Connection.Url, _config.V1Connection.Username));
            }
        }

        ////Check if the asset has been closed.
        //public bool CheckForClosedAsset(string AssetID)
        //{
        //    bool isClosed = false;
        //    IAssetType assetType = _v1Meta.GetAssetType("Workitem");
        //    Query query = new Query(assetType);

        //    IAttributeDefinition stateAttribute = assetType.GetAttributeDefinition("AssetState");
        //    query.Selection.Add(stateAttribute);

        //    IAttributeDefinition numberAttribute = assetType.GetAttributeDefinition("Number");
        //    FilterTerm idFilter = new FilterTerm(numberAttribute);
        //    idFilter.Equal(AssetID);
        //    query.Filter = idFilter;

        //    QueryResult result = _v1Data.Retrieve(query);

        //    if (result.Assets.Count > 0)
        //    {
        //        Asset asset = result.Assets[0];
        //        if (asset.GetAttribute(stateAttribute).Value.ToString() == "Closed") isClosed = true;
        //    }
        //    return isClosed;
        //}

    }
}
