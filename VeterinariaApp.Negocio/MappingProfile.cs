using AutoMapper;
using VeterinariaApp.Entidades;
using VeterinariaApp.Entidades.DTOs;
namespace VeterinariaApp.Negocio;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Cliente, ClienteDto>();
        CreateMap<ClienteCrearDto, Cliente>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Mascotas, o => o.Ignore());
        CreateMap<ClienteActualizarDto, Cliente>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Mascotas, o => o.Ignore());
        CreateMap<Mascota, MascotaDto>()
            .ForMember(d => d.NombreCliente, o => o.MapFrom(src => src.Cliente.Nombre + " " + src.Cliente.Apellido));
        CreateMap<MascotaCrearDto, Mascota>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Cliente, o => o.Ignore());
        CreateMap<MascotaActualizarDto, Mascota>()
            .ForMember(d => d.Id, o => o.Ignore())
            .ForMember(d => d.Cliente, o => o.Ignore());
    }
}
