using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cudumar.Services {
  public static class ServiceManager {
    #region Fields

    private static readonly List<object> services = new List<object>();

    #endregion Fields

    #region Constructors

    static ServiceManager() {
     /* AddService(new FitEng.Base.Logging.LoggingServices.NullLoggingService());
      AddService(new FitEng.Base.Services.TempPathProvision.SystemTempPathProvisionService());
      AddService(new FitEng.Base.Services.Compression.GZipCompressionService());
      AddService(new FitEng.Base.Services.SymmetricEncryption.TripleDESSymmetricEncryptionService());
      AddService(new FitEng.Base.Imaging.ImagingServices.WindowsImagingService());
      AddService(new FitEng.Base.Imaging.ImageEffectServices.NullImageEffectService());
      AddService(new FitEng.Base.Imaging.ImageMetadataServices.WindowsImageMetadataService());
      AddService(new FitEng.Base.Services.MessageBox.WindowsMessageBoxService());
      AddService(new FitEng.Base.Services.DirectorySelectionDialog.WindowsFormsDirectorySelectionDialogService());
      AddService(new FitEng.Base.Services.FileSelectionDialog.Win32FileSelectionDialogService());
      AddService(new FitEng.Base.Services.UsedDirectory.InMemoryUsedDirectoryService());*/
    }

    #endregion Constructors

    #region Methods

    public static void AddService(object service) {
      services.Add(service);
    }

    public static T GetService<T>() {
      return (T)services.FindLast(service => typeof(T).IsInstanceOfType(service));
    }

    #endregion Methods
  }
}
