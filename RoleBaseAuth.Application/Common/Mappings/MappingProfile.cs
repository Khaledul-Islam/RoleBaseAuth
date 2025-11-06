using AutoMapper;
using RoleBaseAuth.Application.DTOs.Auth;
using RoleBaseAuth.Application.DTOs.Customers;
using RoleBaseAuth.Domain.Entities;


namespace RoleBaseAuth.Application.Common.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Roles, opt => opt.MapFrom(src =>
                src.UserRoles.Select(ur => ur.Role.Name).ToList()))
            .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src =>
                src.UserRoles.SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.Name).Distinct().ToList()));

        // Customer mappings
        CreateMap<Customer, CustomerDto>();
        CreateMap<CreateCustomerRequest, Customer>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CustomerCode, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

        CreateMap<UpdateCustomerRequest, Customer>()
            .ForMember(dest => dest.CustomerCode, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore());
    }
}