# DynamoMEP

DynamoMEP is a Dynamo package for working with MEP elements in Revit 2016 and Dynamo 0.9.

DynamoMEP is an Open-Source project available on github and Dynamo’s package manager. The package only contains nodes from creating room and MEP Spaces, and will be improved over time.

The project is being developed in C# using Visual Studio, and will work with Dynamo 0.9.0, and Revit 2016. The project consists of a zero-touch library containing the nodes.

##Nodes

Here is a short description of the existing nodes:

###Room

####Create a room
* Boint(Point): Create a room from its location point and place it on the nearest level.
* ByPointAndLevel(Point,Level): Create a room from its location point and level and place it in the project.
* FromElement(Element): Retrieve a room from an existing one. Use a Dynamo Revit element as input.

####Get metadata
* GetIdentificationData(): Retrieve room name and number

####Get informations
* BoundaryElements: Retrieve the room boundary elements (walls, column and boundary lines).
* Level: Retrieve the room's level.
* LocationPoint: Retrieve the room's location as a Dynamo point.
* Windows: Retrieve windows in this room
* Doors: Retrieve doors in this room


###Space

####Create a MEP Space
* ByPoint(Point): Create a space from its location point and place it on the nearest level.
* ByPointAndLevel(Point,Level): Create a space from its location point and level and place it in the project.
* FromElement(Element) : Retrieve a space from an existing one. Use a Dynamo Revit element as input.

####Get metadata
* GetIdentificationData(): Retrieve space name and number, along with the name and number of the corresponding room.

####Get informations
* BoundaryElements: Retrieve the space boundary elements (walls, column and boundary lines).
* Level: Retrieve the space's level.
* LocationPoint: Retrieve the space's location as a Dynamo point.

##Contribution
All contribution are welcome, of course

##Installation
Load the dll in Dynamo or use Dynamo's package manager.

