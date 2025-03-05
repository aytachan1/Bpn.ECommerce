# Bpn.ECommerce

Bpn.ECommerce is an e-commerce application built with .NET 8. It includes various features such as product management, order processing, and balance management. The project is structured into multiple layers including WebAPI, Application, Domain, and Infrastructure.

## Features

- Product Management
- Order Processing
- Balance Management
- JWT Authentication

## Technologies Used

- .NET 8
- ASP.NET Core
- MediatR
- FluentValidation
- AutoMapper
- Polly
- xUnit
- Moq
- NSubstitute
- Coverlet

## Getting Started

### Prerequisites

- .NET 8 SDK
- Visual Studio 2022 or later

### Installation

1. Clone the repository:
   
### Database Setup

1. Open the solution in Visual Studio.
2. Open the __Package Manager Console__ from __Tools > NuGet Package Manager > Package Manager Console__.
3. Set the default project to `Bpn.ECommerce.Infrastructure` in the Package Manager Console.
4. Run the following commands to create and update the database:"add-migration xx" and "update-database"

      These commands will create the database and tables on SQL Express. If you encounter any errors, check your connection settings in `appsettings.json`.

### Running the Application

1. Set the `Bpn.ECommerce.WebAPI` project as the startup project.
2. Run the application (F5 or Ctrl+F5).

### Using Swagger for API Documentation

1. Open the browser and navigate to `https://localhost:<port>/swagger`.
2. For authentication, use the `login` method with the following credentials:
   - Username: `admin`
   - Password: `1`
3. Copy the token returned from the login method.
4. Click on the __Authorize__ button at the top right corner of the Swagger UI and enter the token.
5. You can use the services
   
   
   
