﻿using Dino.GraphqlLib.Mutations;
using Dino.GraphqlLib.SchemaContexts;
using EntityGraphQL.Schema;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Dino.GraphqlLib.Infrastructures
{
    public class GraphqlFieldBuilder<TSchemaContext>
        where TSchemaContext : ISchemaContext
    {
        private readonly SchemaProvider<TSchemaContext> SchemaProvider;
        private readonly IServiceCollection _services;
        public GraphqlFieldBuilder(IServiceCollection serviceDescriptors, SchemaProvider<TSchemaContext> schemaProvider)
        {
            SchemaProvider = schemaProvider;
            _services = serviceDescriptors;


        }
        public GraphqlMutationBuilder<TSchemaContext> AddMutationBuilder<TDbContextService>()
        {
            _services.AddScoped(typeof(IDbContextService<,,>), typeof(TDbContextService));
            return new(SchemaProvider);
        }
        public GraphqlMutationBuilder<TSchemaContext> AddMutationBuilder(Type dbContextServiceType)
        {
            _services.AddScoped(typeof(IDbContextService<,,>), dbContextServiceType);
            return new(SchemaProvider);
        }
        public GraphqlFieldBuilder<TSchemaContext> RemoveField<TModel>()
        {
            var temps = SchemaProvider.Query()
              .GetFields()
              .Where(x => x.ReturnType.SchemaType.TypeDotnet == typeof(TModel));

            foreach (var item in temps)
            {
                SchemaProvider.Query().RemoveField(item.Name);
            }

            return this;
        }


        private bool AllowNewFieldName(string fieldName)
        {
            if (char.IsLower(fieldName[0]))
            {
                return true;
            }
            throw new ArgumentException($"newField '{fieldName}' is invalid!");
        }
        public FieldToResolve<TModel> ExtendField<TModel>(string newField)
        {
            AllowNewFieldName(newField);
            var field = SchemaProvider.Type<TModel>();
            return field.AddField(newField, null);
        }
        public FieldToResolve<TModel> ReplaceField<TModel>(string nameField)
        {
            var field = SchemaProvider.Type<TModel>();
            return field.ReplaceField(nameField, null);
        }
    }
}
