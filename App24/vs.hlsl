cbuffer ConstantBuffer : register(b0)
{
	matrix view;
	matrix projection;
};

cbuffer PerModelConstantBuffer : register(b1)
{
	matrix world;
};

float4 main(min16float3 inPos : POSITION) : SV_POSITION
{
	float4 pos = float4(inPos, 1.0f);

	pos = mul(pos, world);
	pos = mul(pos, view);
	pos = mul(pos, projection);

	return(pos);
}
