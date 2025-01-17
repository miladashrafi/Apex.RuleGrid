# Apex.RuleGrid  
FREE Excel-based minimal and simple rule-engine that can be integrated into any application.

## Overview

The RuleGrid project is a .NET 8 **Excel-Based** application designed to manage and apply business rules stored in MongoDB. It provides functionality to upload rule sets from Excel files, convert them to JSON, and store them in a MongoDB database. Additionally, it allows applying these rules to a set of objects.

## Features

- Upload rule sets from Excel files.
- Convert Excel data to JSON format.
- Store and retrieve rule sets from MongoDB.
- Apply rules to objects based on specified conditions and actions.

## Technologies Used

- .NET 8
- C# 12.0
- ASP.NET Core
- MongoDB
- ClosedXML (for Excel file handling)
- Swashbuckle (for API documentation)

## Project Structure

### Controllers

- **RuleGridController.cs**: Handles the uploading of rule sets and applying rules to objects.

### Services

- **MongoDbService.cs**: Provides methods to interact with MongoDB, including saving and retrieving rule sets.
- **RuleEngineService.cs**: This is the core service of rule processor and applies rules to objects.

### Models

- **RuleSetDbModel.cs**: Represents the rule set stored in MongoDB.
- **Rule.cs**: Represents an individual rule with conditions and actions.
- **MetaData.cs**: Represents metadata associated with a rule set.
- **RuleApplicationRequest.cs**: Represents the request model for applying rules.

## Getting Started

### Prerequisites

- .NET 8 SDK
- MongoDB instance
- Visual Studio 2022

### Configuration

1. **MongoDB Configuration**: Update the `appsettings.json` file with your MongoDB connection string and database name.

    ```json
    {
        "MongoDB": {
            "ConnectionString": "your-mongodb-connection-string",
            "DatabaseName": "your-database-name"
        }
    }
    ```

### Excel File Format

> - The Excel file should contain two sheets: Metadata and Rules.  
> - The Metadata sheet should contain the following columns: `Id`, `Name`, `ClassName`, `GeneralAction`, `ConditionsOperator`, `Priority`.  
> - The Rules sheet should contain the following columns: `Index`, `Condition_X`, `Action_X`.  
> - The `X` in `Condition_X` and `Action_X` represents the index of the condition or action.  
> - The `ConditionsOperator` column in the Metadata sheet should contain one of the following values: `AND`, `OR`.  
> - The `Priority` column in the Metadata sheet should contain an integer value.  
> - The `GeneralAction` column in the Metadata sheet can contain one of the following values: `SetAppliedRules`.  
> - The `Index` column in the Rules sheet should contain an integer value or `#FieldName`/`#Operator` for first two rows.  
> - The `Condition_X` column in the Rules sheet should contain the condition expression.  
> - The `Action_X` column in the Rules sheet should contain the action expression.  
> - The `ClassName` column in the Metadata sheet should contain the class name to which the rules will be applied.  
> - The `Name` column in the Metadata sheet should contain the name of the rule set.  
> - The `Id` column in the Metadata sheet should contain a unique identifier for the rule set.  
> - Conditions and actions can be one or more columns in the Rules sheet.  
> - The `ConditionsOperator` column in the Metadata sheet should contain the operator to be used for combining conditions.  
> - The `Priority` column in the Metadata sheet should contain the priority of the rule set.  
> - Conditions operator can be `Equals`, `GreaterThan`, `LowerThan`.  
> - Actions operator can be `Set`, `Increase`, `Decrease`.

#### Sheet one (Metadata):
![Excel Sheet One](excel-sheet-one.png)

#### Sheet two (Rules):
![Excel](excel.png)

### Running the Application

1. Open the solution in Visual Studio 2022.
2. Build the solution to restore the dependencies.
3. Run the application.

### API Endpoints

#### Upload Rule Set
- **Endpoint**: `POST /api/v1/RuleEngine/upload-ruleset`
- **Description**: Uploads an Excel file containing rule sets.
- **Request**: `multipart/form-data` with one or more files.

#### Apply Rules

- **Endpoint**: `POST /api/v1/RuleEngine/apply-rules`
- **Description**: Applies rules to a set of objects.
- **Request**: JSON body containing `ClassName` and `Objects`.

    ```json
    {
      "ClassName": "example-class",
      "Objects": [
            {
              "property1": "value1",
              "property2": "value2"
            }
      ]
    }
    ```

### Workflow Diagram

```mermaid
graph TD
    A[Client Request] -->|Upload Rule Set| B[RuleGridController]
    B -->|Extract Data from Excel| C[ClosedXML]
    C -->|Convert to JSON| D[RuleSetDbModel]
    D -->|Store in MongoDB| E[MongoDbService]
    A -->|Apply Rules| B
    B -->|Fetch Rules from MongoDB| E
    E -->|Apply Conditions and Actions| F[Rule Processor]
    F -->|Return Modified Objects| G[Client Response]
```

## Example Usage

### Uploading a Rule Set

1. Use a tool like Postman to send a `POST` request to `/api/v1/RuleEngine/upload-ruleset`.
2. Attach the Excel file(s) containing the rule sets.

### Applying Rules

1. Use a tool like Postman to send a `POST` request to `/api/v1/RuleEngine/apply-rules`.
2. Provide the `ClassName` and `Objects` in the request body.

## Contributing

Contributions are welcome! Please open an issue or submit a pull request for any improvements or bug fixes.

## License

This project is licensed under the MIT License.

## Contact

For any questions or support, please contact the project maintainers.

[@miladashrafi](https://github.com/miladashrafi/)

---

This README file provides a comprehensive overview of the RuleGrid project, including its features, technologies used, project structure, and instructions for getting started.

