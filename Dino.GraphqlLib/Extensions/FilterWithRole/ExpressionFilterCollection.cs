﻿using Dino.GraphqlLib.Mutations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Dino.GraphqlLib.Extensions.FilterWithRole
{
    public interface IExpressionFilterCollection
    {
        IExpressionFilterCollection AddWhereClause<TWhereClause, TModel>() where TModel : class where TWhereClause : class, IExpressionFilter<TModel>;
        IExpressionFilterCollection AddWhereClause<TModel>(Func<IServiceProvider, Expression<Func<TModel, bool>>> action) where TModel : class;
        IExpressionFilterCollection AddAuthorizeWhereClause<TModel>(Action<IServiceProvider, IWhereClauseAuthorizeCollection<TModel>> config) where TModel : class;
        IExpressionFilterCollection AddSiteClaimTransformation<T>() where T : CallbackAttachSite;
        IExpressionFilterCollection AddSiteRoleTransformation(Func<HttpContext, IEnumerable<string>> Action);
    }

    public class ExpressionFilterCollection : IExpressionFilterCollection
    {
        public ExpressionFilterCollection()
        {
            _instance = this;
        }
        private static ExpressionFilterCollection _instance;
        public static ExpressionFilterCollection Instance { get => _instance == null ? new ExpressionFilterCollection() : _instance; }
        public static void Clear()
        {
            _instance = null;
        }
        //Service

        public IServiceCollection Services { get; set; }
        public void SetupService(IServiceCollection services)
        {
            Services = services;
        }

        public IExpressionFilterCollection AddWhereClause<TWhereClause, TModel>()
           where TModel : class
           where TWhereClause : class, IExpressionFilter<TModel>
        {
            Services.AddScoped<IExpressionFilter<TModel>, TWhereClause>();
            return this;
        }
        public IExpressionFilterCollection AddWhereClause<TModel>(Expression<Func<TModel, bool>> expression)
           where TModel : class
        {
            Services.AddScoped<IExpressionFilter<TModel>>(x => ActivatorUtilities.CreateInstance<ExpressionFilterDefault<TModel>>(x, expression));
            return this;
        }
        public IExpressionFilterCollection AddWhereClause<TModel>(Func<IServiceProvider, Expression<Func<TModel, bool>>> action)
           where TModel : class
        {
            Services.AddScoped<IExpressionFilter<TModel>>(x => ActivatorUtilities.CreateInstance<ExpressionFilterDefault<TModel>>(x, action(Provider)));
            return this;
        }


        public IExpressionFilterCollection AddAuthorizeWhereClause<TModel>(Action<IServiceProvider, IWhereClauseAuthorizeCollection<TModel>> config) where TModel : class
        {
            var builder = new ExpressionAuthorizeCollection<TModel>();
            config(Provider, builder);
            Services.AddScoped<IExpressionFilter<TModel>>(builder.GetService);
            return this;
        }

        public IExpressionFilterCollection AddSiteClaimTransformation<T>() where T : CallbackAttachSite
        {
            Services.AddTransient<IClaimsTransformation, AttachSiteToClaimsTransformation>();
            Services.AddTransient<CallbackAttachSite, T>();
            return this;
        }

        public IExpressionFilterCollection AddSiteRoleTransformation(Func<HttpContext,IEnumerable<string>> Action)
        {
            Services.AddTransient<IClaimsTransformation, AttachSiteToClaimsTransformation>();
            Services.AddTransient(p => new CallbackAttachSite() { GetSite = Action });
            return this;
        }


        //Provider
        public IServiceProvider Provider { get; set; }
        public void SetupProvider(IServiceProvider serviceProvider)
        {
            Provider = serviceProvider;
        }

        public Expression GetExpression<TModel>(Expression expression)
            where TModel : class
        {
            var expressionFilter = Provider.GetService<IExpressionFilter<TModel>>();
            return expressionFilter?.GetExpression(expression);
        }
        public Expression GetExpression(Type type, Expression expression)
        {
            var method = GetType().GetMethod(nameof(GetExpression), 1, new Type[] { typeof(Expression) }).MakeGenericMethod(type);

            return method.Invoke(this, new[] { expression }) as Expression;
        }


    }

}
