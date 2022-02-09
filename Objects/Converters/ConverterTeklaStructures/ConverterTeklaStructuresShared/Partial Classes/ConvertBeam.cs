﻿using System;
using System.Collections.Generic;
using Objects.Geometry;
using Objects.Structural.Geometry;
using Objects.Structural.Analysis;
using Speckle.Core.Models;
using BE = Objects.BuiltElements;
using Objects.BuiltElements.TeklaStructures;
using System.Linq;
using Tekla.Structures.Model;
using Tekla.Structures.Solid;
using TSG = Tekla.Structures.Geometry3d;
using System.Collections;
using StructuralUtilities.PolygonMesher;

namespace Objects.Converter.TeklaStructures
{
  public partial class ConverterTeklaStructures
  {
    public void BeamToNative(BE.Beam beam)
    {


      if (beam is TeklaBeam)
      {

        var teklaBeam = (TeklaBeam)beam;
        switch (teklaBeam.TeklaBeamType)
        {
          case TeklaBeamType.Beam:
            if (!(beam.baseLine is Line))
            {
            }
            Line line = (Line)beam.baseLine;
            TSG.Point startPoint = new TSG.Point(line.start.x, line.start.y, line.start.z);
            TSG.Point endPoint = new TSG.Point(line.end.x, line.end.y, line.end.z);
            var myBeam = new Beam(startPoint, endPoint);
            SetBeamProperties(myBeam, teklaBeam);
            myBeam.Insert();
            Model.CommitChanges();
            break;
          case TeklaBeamType.PolyBeam:
            Polyline polyline = (Polyline)beam.baseLine;
            var polyBeam = new PolyBeam();
            ToNativeContour(polyline, polyBeam.Contour);
            SetBeamProperties(polyBeam, teklaBeam);
            polyBeam.Insert();
            Model.CommitChanges();
            break;
          case TeklaBeamType.SpiralBeam:
            Polyline polyline2 = (Polyline)beam.baseLine;
            var teklaSpiralBeam = (Objects.BuiltElements.TeklaStructures.SpiralBeam)teklaBeam;
            var startPt = new TSG.Point(teklaSpiralBeam.startPoint.x, teklaSpiralBeam.startPoint.y, teklaSpiralBeam.startPoint.z);
            var rotatAxisPt1 = new TSG.Point(teklaSpiralBeam.rotationAxisPt1.x, teklaSpiralBeam.rotationAxisPt1.y, teklaSpiralBeam.rotationAxisPt1.z);
            var rotatAxisPt2 = new TSG.Point(teklaSpiralBeam.rotationAxisPt2.x, teklaSpiralBeam.rotationAxisPt2.y, teklaSpiralBeam.rotationAxisPt2.z);
            var totalRise = teklaSpiralBeam.totalRise;
            var rotationAngle = teklaSpiralBeam.rotationAngle;
            var twistAngleStart = teklaSpiralBeam.twistAngleStart;
            var twistAngleEnd = teklaSpiralBeam.twistAngleEnd;
            var spiralBeam = new Tekla.Structures.Model.SpiralBeam(startPt, rotatAxisPt1, rotatAxisPt2, totalRise, rotationAngle, twistAngleStart, twistAngleEnd);
            SetBeamProperties(spiralBeam, teklaBeam);
            spiralBeam.Insert();
            Model.CommitChanges();
            break;
        }
      }
      else
      {
        if (!(beam.baseLine is Line))
        {
        }
        Line line = (Line)beam.baseLine;
        TSG.Point startPoint = new TSG.Point(line.start.x, line.start.y, line.start.z);
        TSG.Point endPoint = new TSG.Point(line.end.x, line.end.y, line.end.z);
        var myBeam = new Beam(startPoint, endPoint);
        myBeam.Insert();
        Model.CommitChanges();
      }
    }

    public void SetBeamProperties(Part beam, TeklaBeam teklaBeam)
    {
      beam.Material.MaterialString = teklaBeam.material.name;
      beam.Profile.ProfileString = teklaBeam.profile.name;
      beam.Class = teklaBeam.classNumber;
      beam.Finish = teklaBeam.finish;
      beam.Name = teklaBeam.name;
    }
    public TeklaBeam BeamToSpeckle(Tekla.Structures.Model.Beam beam)
    {
      var speckleBeam = new TeklaBeam();
      //TO DO: Support for curved beams goes in here as well + twin beams

      var endPoint = beam.EndPoint;
      var startPoint = beam.StartPoint;
      var units = GetUnitsFromModel();

      Point speckleStartPoint = new Point(startPoint.X, startPoint.Y, startPoint.Z, units);
      Point speckleEndPoint = new Point(endPoint.X, endPoint.Y, endPoint.Z, units);
      speckleBeam.baseLine = new Line(speckleStartPoint, speckleEndPoint, units);

      speckleBeam.profile = GetProfile(beam.Profile.ProfileString);
      speckleBeam.material = GetMaterial(beam.Material.MaterialString);
      var beamCS = beam.GetCoordinateSystem();
      speckleBeam.alignmentVector = new Vector(beamCS.AxisY.X, beamCS.AxisY.Y, beamCS.AxisY.Z, units);
      speckleBeam.finish = beam.Finish;
      speckleBeam.classNumber = beam.Class;
      speckleBeam.name = beam.Name;
      speckleBeam.TeklaBeamType = TeklaBeamType.Beam;

      GetAllUserProperties(speckleBeam, beam);

      var solid = beam.GetSolid();
      speckleBeam.displayMesh = GetMeshFromSolid(solid);
      return speckleBeam;
    }
  }
}