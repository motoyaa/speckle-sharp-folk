﻿using System;
using System.Collections.Generic;
using System.Text;
using Speckle.Core.Models;
using Objects.Structural.Properties;
using Speckle.Core.Kits;

namespace Objects.Structural.CSI.Properties
{
  public class CSIDiaphragm : Base
  {
  public string name { get; set; }
  public bool SemiRigid { get; set; }

    [SchemaInfo("CSI Diaphragm", "Create an CSI Diaphragm", "CSI", "Properties")]
    public CSIDiaphragm(string name, bool semiRigid)
    {
      this.name = name;
      SemiRigid = semiRigid;
    }

    public CSIDiaphragm()
    {
    }
  }
}
