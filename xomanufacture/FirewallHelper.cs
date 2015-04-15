using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using NATUPNPLib;
using NETCONLib;
using NetFwTypeLib;

namespace xomanufacture
{
/// Allows basic access to the windows firewall API.
/// This can be used to add an exception to the windows firewall
/// exceptions list
public class FirewallHelper
{
    #region Variables

    /// Hooray! Singleton access.
    private static FirewallHelper instance;

    /// Interface to the firewall manager COM object
    private INetFwMgr fireWallManager = null;
    public INetFwPolicy2 FWPolicy2 = null;


    #endregion

    #region Properties

    /// Singleton access to the firewallhelper object.
    /// Threadsafe.
    public static FirewallHelper Instance
    {
        get
        {
            lock (typeof (FirewallHelper))
            {
                return instance ?? (instance = new FirewallHelper());
            }
        }
    }

    #endregion

    #region Constructivat0r

    /// Private Constructor.  
    /// If this fails, HasFirewall will return false
    private FirewallHelper()
    {
        // Get the type of HNetCfg.FwMgr, or null if an error occurred
        Type fwMgrType = Type.GetTypeFromProgID("HNetCfg.FwMgr", false);
        Type tNetFwPolicy2 = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");

        // Assume failed.
        fireWallManager = null;

        if (fwMgrType != null)
        {
            try
            {
                fireWallManager =
                    (INetFwMgr) Activator.CreateInstance(fwMgrType);
                FWPolicy2 =
                    (INetFwPolicy2)Activator.CreateInstance(tNetFwPolicy2);
            }
                // In all other circumnstances, fireWallManager is null.
            catch (ArgumentException)
            {
            }
            catch (NotSupportedException)
            {
            }
            catch (TargetInvocationException)
            {
            }
            catch (MissingMethodException)
            {
            }
            catch (MethodAccessException)
            {
            }
            catch (MemberAccessException)
            {
            }
            catch (InvalidComObjectException)
            {
            }
            catch (COMException)
            {
            }
            catch (TypeLoadException)
            {
            }
        }
    }

    #endregion

    #region Helper Methods

    /// Gets whether or not the firewall is installed on this computer.
    public bool IsFirewallInstalled
    {
        get
        {
            return fireWallManager != null &&
                   fireWallManager.LocalPolicy != null &&
                   fireWallManager.LocalPolicy.CurrentProfile != null;
        }
    }

    /// Returns whether or not the firewall is enabled.
    /// If the firewall is not installed, this returns false.
    public bool IsFirewallEnabled
    {
        get
        {
            return IsFirewallInstalled &&
                   fireWallManager.LocalPolicy.CurrentProfile.
                       FirewallEnabled;
        }
    }

    /// Returns whether or not the firewall allows Application "Exceptions".
    /// If the firewall is not installed, this returns false.
    /// 
    /// 
    /// Added to allow access to this method
    public bool AppAuthorizationsAllowed
    {
        get
        {
            return IsFirewallInstalled &&
                   !fireWallManager.LocalPolicy.CurrentProfile.
                        ExceptionsNotAllowed;
        }
    }

    /// Adds an application to the list of authorized applications.
    /// If the application is already authorized, does nothing.
    /// 
    /// 
    /// The full path to the application executable.  This cannot
    /// be blank, and cannot be a relative path.
    /// 
    /// 
    /// This is the name of the application, purely for display
    /// puposes in the Microsoft Security Center.
    /// 
    /// 
    /// When applicationFullPath is null OR
    /// When appName is null.
    /// 
    /// 
    /// When applicationFullPath is blank OR
    /// When appName is blank OR
    /// applicationFullPath contains invalid path characters OR
    /// applicationFullPath is not an absolute path
    /// 
    /// 
    /// If the firewall is not installed OR
    /// If the firewall does not allow specific application 'exceptions' OR
    /// Due to an exception in COM this method could not create the
    /// necessary COM types
    /// 
    /// 
    /// If no file exists at the given applicationFullPath
    public void GrantAuthorization(string applicationFullPath,
                                   string appName,
                                   NET_FW_SCOPE_ scope,
                                   NET_FW_IP_VERSION_ ipVersion)
    {
        #region  Parameter checking

        if (applicationFullPath == null)
            throw new ArgumentNullException("applicationFullPath");
        if (appName == null)
            throw new ArgumentNullException("appName");
        if (applicationFullPath.Trim().Length == 0)
            throw new ArgumentException(
                "applicationFullPath must not be blank");
        if (applicationFullPath.Trim().Length == 0)
            throw new ArgumentException("appName must not be blank");
        if (applicationFullPath.IndexOfAny(Path.InvalidPathChars) >= 0)
            throw new ArgumentException(
                "applicationFullPath must not contain invalid path characters");
        if (!Path.IsPathRooted(applicationFullPath))
            throw new ArgumentException(
                "applicationFullPath is not an absolute path");
        if (!File.Exists(applicationFullPath))
            throw new FileNotFoundException("File does not exist",
                                            applicationFullPath);

        // State checking
        if (!IsFirewallInstalled)
            throw new FirewallHelperException(
                "Cannot grant authorization: Firewall is not installed.");
        if (!AppAuthorizationsAllowed)
            throw new FirewallHelperException(
                "Application exemptions are not allowed.");

        #endregion

        if (!HasAuthorization(applicationFullPath))
        {
            // Get the type of HNetCfg.FwMgr, or null if an error occurred
            Type authAppType =
                Type.GetTypeFromProgID("HNetCfg.FwAuthorizedApplication",
                                       false);

            // Assume failed.
            INetFwAuthorizedApplication appInfo = null;

            if (authAppType != null)
            {
                try
                {
                    appInfo =
                        (INetFwAuthorizedApplication)
                        Activator.CreateInstance(authAppType);
                }
                    // In all other circumnstances, appInfo is null.
                catch (ArgumentException)
                {
                }
                catch (NotSupportedException)
                {
                }
                catch (TargetInvocationException)
                {
                }
                catch (MissingMethodException)
                {
                }
                catch (MethodAccessException)
                {
                }
                catch (MemberAccessException)
                {
                }
                catch (InvalidComObjectException)
                {
                }
                catch (COMException)
                {
                }
                catch (TypeLoadException)
                {
                }
            }

            if (appInfo == null)
                throw new FirewallHelperException(
                    "Could not grant authorization: can't create INetFwAuthorizedApplication instance.");

            appInfo.Name = appName;
            appInfo.ProcessImageFileName = applicationFullPath;
            appInfo.Scope = scope;
            appInfo.IpVersion = ipVersion;
            appInfo.Enabled = true;
            // ...
            // Use defaults for other properties of the AuthorizedApplication COM object

            // Authorize this application
            fireWallManager.LocalPolicy.CurrentProfile.
                AuthorizedApplications.Add(appInfo);
        }
        // otherwise it already has authorization so do nothing
    }

    /// Removes an application to the list of authorized applications.
    /// Note that the specified application must exist or a FileNotFound
    /// exception will be thrown.
    /// If the specified application exists but does not current have
    /// authorization, this method will do nothing.
    /// 
    /// 
    /// The full path to the application executable.  This cannot
    /// be blank, and cannot be a relative path.
    /// 
    /// 
    /// When applicationFullPath is null
    /// 
    /// 
    /// When applicationFullPath is blank OR
    /// applicationFullPath contains invalid path characters OR
    /// applicationFullPath is not an absolute path
    /// 
    /// 
    /// If the firewall is not installed.
    /// 
    /// 
    /// If the specified application does not exist.
    public void RemoveAuthorization(string applicationFullPath)
    {
        #region  Parameter checking

        if (applicationFullPath == null)
            throw new ArgumentNullException("applicationFullPath");
        if (applicationFullPath.Trim().Length == 0)
            throw new ArgumentException(
                "applicationFullPath must not be blank");
        if (applicationFullPath.IndexOfAny(Path.InvalidPathChars) >= 0)
            throw new ArgumentException(
                "applicationFullPath must not contain invalid path characters");
        if (!Path.IsPathRooted(applicationFullPath))
            throw new ArgumentException(
                "applicationFullPath is not an absolute path");
        if (!File.Exists(applicationFullPath))
            throw new FileNotFoundException("File does not exist",
                                            applicationFullPath);
        // State checking
        if (!IsFirewallInstalled)
            throw new FirewallHelperException(
                "Cannot remove authorization: Firewall is not installed.");

        #endregion

        if (HasAuthorization(applicationFullPath))
        {
            // Remove Authorization for this application
            fireWallManager.LocalPolicy.CurrentProfile.
                AuthorizedApplications.Remove(applicationFullPath);
        }
        // otherwise it does not have authorization so do nothing
    }

    /// Returns whether an application is in the list of authorized applications.
    /// Note if the file does not exist, this throws a FileNotFound exception.
    /// 
    /// 
    /// The full path to the application executable.  This cannot
    /// be blank, and cannot be a relative path.
    /// 
    /// 
    /// The full path to the application executable.  This cannot
    /// be blank, and cannot be a relative path.
    /// 
    /// 
    /// When applicationFullPath is null
    /// 
    /// 
    /// When applicationFullPath is blank OR
    /// applicationFullPath contains invalid path characters OR
    /// applicationFullPath is not an absolute path
    /// 
    /// 
    /// If the firewall is not installed.
    /// 
    /// 
    /// If the specified application does not exist.
    public bool HasAuthorization(string applicationFullPath)
    {
        #region  Parameter checking

        if (applicationFullPath == null)
            throw new ArgumentNullException("applicationFullPath");
        if (applicationFullPath.Trim().Length == 0)
            throw new ArgumentException(
                "applicationFullPath must not be blank");
        if (applicationFullPath.IndexOfAny(Path.InvalidPathChars) >= 0)
            throw new ArgumentException(
                "applicationFullPath must not contain invalid path characters");
        if (!Path.IsPathRooted(applicationFullPath))
            throw new ArgumentException(
                "applicationFullPath is not an absolute path");
        if (!File.Exists(applicationFullPath))
            throw new FileNotFoundException("File does not exist.",
                                            applicationFullPath);
        // State checking
        if (!IsFirewallInstalled)
            throw new FirewallHelperException(
                "Cannot remove authorization: Firewall is not installed.");

        #endregion

        // Locate Authorization for this application
        return
            GetAuthorizedAppPaths().Cast<string>().Any(
                appName =>
                appName.ToLower() == applicationFullPath.ToLower());

        // Failed to locate the given app.
    }

    /// Retrieves a collection of paths to applications that are authorized.
    /// 
    /// 
    /// 
    /// If the Firewall is not installed.
    public ICollection GetAuthorizedAppPaths()
    {
        // State checking
        if (!IsFirewallInstalled)
            throw new FirewallHelperException(
                "Cannot remove authorization: Firewall is not installed.");

        ArrayList list = new ArrayList();
        //  Collect the paths of all authorized applications
        foreach (
            INetFwAuthorizedApplication app in
                fireWallManager.LocalPolicy.CurrentProfile.
                    AuthorizedApplications)
        {
            list.Add(app.ProcessImageFileName);
        }

        return list;
    }

    #endregion

    public void SetFirewallStatus(bool _newstatus)
    {
        NET_FW_PROFILE_TYPE2_ ProfileType;
        ProfileType = NET_FW_PROFILE_TYPE2_.NET_FW_PROFILE2_PRIVATE;
        FWPolicy2.set_FirewallEnabled(ProfileType, _newstatus);
    }
}

/// 
/// Describes a FirewallHelperException.
/// 
public class FirewallHelperException : Exception
{
    /// 
    /// Construct a new FirewallHelperException
    /// 
    /// 
    public FirewallHelperException(string message)
        : base(message)
    { }
}

}
