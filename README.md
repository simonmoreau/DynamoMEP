
<p align="center"><img width=12.5% src="https://github.com/simonmoreau/DynamoMEP/blob/master/assets/DynamoMEP.About.AboutDynamoMEP.Large.png"></p>
<h1 align="center">
  DynamoMEP
</h1>

<h4 align="center">A Dynamo package for MEP elements</h4>

# Overview

DynamoMEP is a Dynamo package for working with MEP elements in Revit.

DynamoMEP is an Open-Source project available on github and Dynamo’s package manager. The package started with nodes for creating room and MEP Spaces, and will be improved over time.

The project is being developed in C# using Visual Studio, and will work with Dynamo 2.0 and forward, and Revit 2016 and forward. The project mostly consists of zero-touch nodes with a few custom ones.

![Overview](https://github.com/simonmoreau/DynamoMEP/blob/master/assets/sample.png)

## Installation

Load the dll in Dynamo or use Dynamo's package manager.

## Built With

* .NET Framework 4.8 and [Visual Studio Community](https://www.visualstudio.com/vs/community/)

# Development

Want to contribute? Great, I would be happy to integrate your improvements!

To fix a bug or enhance an existing module, follow these steps:

* Fork the repo
* Create a new branch (`git checkout -b improve-feature`)
* Make the appropriate changes in the files
* Add changes to reflect the changes made
* Commit your changes (`git commit -am 'Improve feature'`)
* Push to the branch (`git push origin improve-feature`)
* Create a Pull Request

# Bug / Feature Request

If you find a bug (connection issue, error while uploading, ...), kindly open an issue [here](https://github.com/simonmoreau/DynamoMEP/issues/new) by including a screenshot of your problem and the expected result.

If you'd like to request a new function, feel free to do so by opening an issue [here](https://github.com/simonmoreau/DynamoMEP/issues/new). Please include workflows samples and their corresponding results.

# License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE) file for details

# Contact information

This software is an open-source project mostly maintained by myself, Simon Moreau. If you have any questions or request, feel free to contact me at [simon@bim42.com](mailto:simon@bim42.com) or on Twitter [@bim42](https://twitter.com/bim42?lang=en).

# Node list

## About
### About.AboutDynamoMEP
The About node

## Area
### Area.BoundaryElements
Retrive area boundary elements

### Area.ByPointAndView
Create a Revit Area based on a location and a view plan

### Area.FromElement
Create a Revit Area from an existing Revit Area

### Area.GetIdentificationData
Retrive a set of properties for the Area

### Area.Level
Retrive the space associated level

### Area.LocationPoint
Retrive the area location

## AreaBoundary
### AreaBoundary.ByCurve
Construct a Revit Area Boundary element from a Curve. The curve will be project onto the Revit active view. The active view must be an Area plan view.

### AreaBoundary.ByCurveAndView
Construct a Revit Area Boundary element from a Curve and a base plan view

## FamillyInstance
### FamillyInstance.FamilyInstanceReferenceType
Select the type of the reference

### FamillyInstance.GetReferencePlaneByName
Get the reference planes of the family instance by name 

### FamillyInstance.GetReferencesNames
Get all the names of the reference planes of the family instance
Get all the reference planes of the family instance

### FamillyInstance.GetReferencesPlanes
Get all the reference planes of the family instance

### FamillyInstance.GetReferencesPlanesByType
Get the reference planes of the family instance by type 

## Group
### Group.FromElement
Create a group from an existing one 

### Group.PlaceGroupInstance
Place a group instance in the project

## GroupType
### GroupType.FromElement
Get an existing group type from the project

### GroupType.FromElements
Create a group type from a list of Revit elements

### GroupType.FromElementsAndName
Create a group type from a list of Revit elements and a name

### GroupType.FromName
Create a named group type

## Room
### Room.BoundaryElements
Get all room boundary elements

### Room.ByPoint
Create a room at a given point

### Room.Doors
Get all doors in the room

### Room.Grid
Create a grid of points in a room

### Room.Windows
Get all windows in the room

## RoomSeparator
### RoomSeparator.ByCurve
Create a room separator line in the current view

### RoomSeparator.ByCurveAndView
Create a room separator line in a given view

## Space
### Space.BoundaryElements
Get all MEP space boundary elements

### Space.ByPoint
Create a MEP space at a given point

### Space.ByPointAndLevel
Create a MEP space at a given point on the given level

### Space.FromElement
Get a MEP space from an existing space

### Space.GetIdentificationData
Get the identification data of the MEP space 

### Space.Grid
Create a grid of points in a MEP space

### Space.IsInSpace
Check if a given point is in the MEP space

### Space.Level
Get the MEP space level

### Space.LocationPoint
Get the MEP space location point

## SpaceSeparator
### SpaceSeparator.ByCurve
Create a MEP space separator line in the current view

### SpaceSeparator.ByCurveAndView
Create a MEP space separator line in a given view

