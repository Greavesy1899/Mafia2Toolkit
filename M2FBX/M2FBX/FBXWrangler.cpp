#include "FbxWrangler.h"
#include "FbxUtilities.h"

bool SaveDocument(FbxManager* pManager, FbxDocument* pDocument, const char* pFilename, int pFileFormat = 1, bool pEmbedMedia = false)
{
	int lMajor, lMinor, lRevision;
	bool lStatus = true;

	// Create an exporter.
	FbxExporter* lExporter = FbxExporter::Create(pManager, "");

	// Set the export states. By default, the export states are always set to 
	// true except for the option eEXPORT_TEXTURE_AS_EMBEDDED. The code below 
	// shows how to change these states.
	IOS_REF.SetBoolProp(EXP_FBX_MATERIAL, true);
	IOS_REF.SetBoolProp(EXP_FBX_TEXTURE, true);
	IOS_REF.SetBoolProp(EXP_FBX_EMBEDDED, pEmbedMedia);
	IOS_REF.SetBoolProp(EXP_FBX_ANIMATION, true);
	IOS_REF.SetBoolProp(EXP_FBX_GLOBAL_SETTINGS, true);

	// Initialize the exporter by providing a filename.
	if (!lExporter->Initialize(pFilename, pFileFormat, pManager->GetIOSettings()))
	{
		WriteLine("Call to FbxExporter::Initialize() failed.");
		WriteLine("Error returned: %s\n", lExporter->GetStatus().GetErrorString());
		return false;
	}

	FbxManager::GetFileFormatVersion(lMajor, lMinor, lRevision);
	WriteLine("FBX version number for this version of the FBX SDK is %d.%d.%d\n", lMajor, lMinor, lRevision);

	// Export the scene.
	lStatus = lExporter->Export(pDocument);

	// Destroy the exporter.
	lExporter->Destroy();
	return lStatus;
}
int ConvertType(const char* pSource, const char* pDest)
{
	FbxManager* lSdkManager = NULL;
	FbxImporter* lImporter = NULL;
	FbxScene* lScene = NULL;

	//Prepare SDK..
	InitializeSdkObjects(lSdkManager);
	lImporter = FbxImporter::Create(lSdkManager, "");
	WriteLine("Loading FBX File..");
	//Init importer. if it fails, it will print error code.
	if (!lImporter->Initialize(pSource, -1, lSdkManager->GetIOSettings())) {
		WriteLine("Error occurred while initializing importer:");
		WriteLine("%s", lImporter->GetStatus().GetErrorString());
		return -1;
	}

	WriteLine("Importing %s...", pSource);

	//Populate scene and destroy importer.
	lScene = FbxScene::Create(lSdkManager, "scene");
	lImporter->Import(lScene);
	int format = lImporter->GetFileFormat();
	lImporter->Destroy();

	WriteLine("Format is %i", format);
	bool lResult = SaveDocument(lSdkManager, lScene, pDest, format == 0 ? 1 : 0);
	DestroySdkObjects(lSdkManager, lResult);
	return 1;
}
int ConvertM2T(const char* pSource, const char* pDest, unsigned char isBin)
{
	WriteLine("Converting M2T to FBX.");

	FbxManager* lSdkManager = nullptr;
	FbxScene* lScene = nullptr;
	bool lResult = false;

	//Open stream.
	FILE* stream;
	fopen_s(&stream, pSource, "rb");

	if (!stream)
		return 0;

	// Load Model data, and close stream.
 	ModelStructure file = ModelStructure();
	file.ReadFromStream(stream);
	fclose(stream);

	// Prepare the FBX SDK.
	InitializeSdkObjects(lSdkManager);

	// create the main document
	lScene = FbxScene::Create(lSdkManager, "Scene");
	// Create the scene.
	lResult = CreateDocument(lSdkManager, lScene, file);
	if (lResult)
	{
		//Save the document
		lResult = SaveDocument(lSdkManager, lScene, pDest, isBin);
		if (!lResult) WriteLine("\n\nAn error occurred while saving the document...");
	}
	else WriteLine("\n\nAn error occurred while creating the document...");

	// Destroy all objects created by the FBX SDK.
	DestroySdkObjects(lSdkManager, lResult);
	return 0;
}
bool CreateDocument(FbxManager* pManager, FbxScene* pScene, ModelStructure model)
{
	int lCount;

	// create document info
	FbxDocumentInfo* lDocInfo = FbxDocumentInfo::Create(pManager, "DocInfo");
	lDocInfo->mTitle = "FBX Model";
	lDocInfo->mSubject = "FBX Created by M2FBX - Used by MafiaToolkit.";
	lDocInfo->mAuthor = "Greavesy";
	lDocInfo->mRevision = "rev. 0.20";
	lDocInfo->mKeywords = "";
	lDocInfo->mComment = "";

	// add the documentInfo
	pScene->SetDocumentInfo(lDocInfo);
	// NOTE: Objects created directly in the SDK Manager are not visible
	// to the disk save routines unless they are manually connected to the
	// documents (see below). Ideally, one would directly use the FbxScene/FbxDocument
	// during the creation of objects so they are automatically connected and become visible
	// to the disk save routines.

	FbxNode* lLodNode = FbxNode::Create(pManager, model.GetName().c_str());
	std::string nodeName = model.GetName();
	nodeName += "_LODNODE";

	for (int i = 0; i < 1; i++)
	{
		std::string name = "LOD";
		name += std::to_string(i);
		FbxNode* lModel = CreatePlane(pManager, name.c_str(), model.GetParts()[i]);
		lLodNode->AddChild(lModel);
	}

	// add the geometry to the main document.	
	//FbxNode* node = pScene->GetRootNode();
	//node->AddChild(lLodGroup);
	FbxNode* rootNode = pScene->GetRootNode();
	rootNode->AddChild(lLodNode);
	pScene->AddNode(rootNode);
	pScene->AddRootMember(rootNode);

	if (model.GetIsSkinned())
	{
		auto names = model.GetJointNames();
		auto joints = model.GetJoints();
		ModelPart* parts = model.GetParts();
		FbxSkin* skin = FbxSkin::Create(pManager, "Skin");

		std::vector<FbxNode*> nodes = std::vector<FbxNode*>();
		std::vector<FbxCluster*> clusters = std::vector<FbxCluster*>();
		for (int i = 0; i < names.size(); i++)
		{
			auto joint = joints[i];
			auto transform = joint.transform;

			FbxString skeletonName = names[i].c_str();
			FbxString clusterName = names[i].c_str();
			FbxString nodeName = names[i].c_str();
			skeletonName.Append("_Skeleton", 9);
			clusterName.Append("_Cluster", 8);

			FbxQuaternion quaterion = FbxQuaternion(transform.rotation.x, transform.rotation.y, transform.rotation.z, transform.rotation.w);

			FbxAMatrix lTransformMatrix;
			lTransformMatrix.SetIdentity();

			lTransformMatrix.SetT(FbxVector4(transform.position.x, transform.position.y, transform.position.z));
			lTransformMatrix.SetQ(quaterion);
			lTransformMatrix.SetS(FbxVector4(transform.scale.x, transform.scale.y, transform.scale.z));

			FbxVector4 euler;
			euler.SetXYZ(quaterion);

			FbxNode* node = FbxNode::Create(pManager, nodeName.Buffer());

			if (joint.parentID != 0xFF)
			{
				nodes[joint.parentID]->AddChild(node);
			}
			else
			{
				rootNode->AddChild(node);
			}

			node->LclTranslation.Set(lTransformMatrix.GetT());
			node->LclScaling.Set(lTransformMatrix.GetS());
			node->LclRotation.Set(euler);

			FbxSkeleton* skeleton = FbxSkeleton::Create(pManager, skeletonName.Buffer());
			skeleton->SetSkeletonType(FbxSkeleton::EType::eLimbNode);

			node->SetNodeAttribute(skeleton);

			FbxCluster* cluster = FbxCluster::Create(pManager, clusterName.Buffer());
			cluster->SetLinkMode(FbxCluster::eTotalOne);
			cluster->SetLink(node);
			cluster->SetTransformLinkMatrix(node->EvaluateGlobalTransform());
			clusters.push_back(cluster);
			skin->AddCluster(cluster);

			nodes.push_back(node);
		}
		for (int i = 0; i < 1; i++)
		{
			auto vertices = parts[i].GetVertices();
			for (int x = 0; x < parts[i].GetVertSize(); x++)
			{
				auto vertex = vertices[x];
				for (int z = 0; z < 4; z++)
				{
					if (vertex.boneWeights[z] != 0.0f)
					{
						clusters[vertex.boneIDs[z]]->AddControlPointIndex(x, vertex.boneWeights[z]);
					}
				}
			}
			auto attribute = lLodNode->GetChild(i)->GetNodeAttribute();
			FbxGeometry* geometry = (FbxGeometry*)attribute;
			geometry->AddDeformer(skin);
		}
	}

	return true;
}
void CreateLightDocument(FbxManager* pManager, FbxDocument* pLightDocument)
{
	// create document info
	FbxDocumentInfo* lDocInfo = FbxDocumentInfo::Create(pManager, "DocInfo");
	lDocInfo->mTitle = "";
	lDocInfo->mSubject = "";
	lDocInfo->mAuthor = "Mafia: Toolkit";
	lDocInfo->mRevision = "rev. 2.0";
	lDocInfo->mKeywords = "";
	lDocInfo->mComment = "";

	// add the documentInfo
	pLightDocument->SetDocumentInfo(lDocInfo);
}
void SetupGlobalSettings(FbxScene* pScene)
{
	// the scene is filled with objects
	int dir;
	pScene->GetGlobalSettings().GetAxisSystem().GetUpVector(dir); // this returns the equivalent of FbxAxisSystem::eYAxis
	FbxAxisSystem max; // we desire to convert the scene from Y-Up to Z-Up
	max.ConvertScene(pScene);
	pScene->GetGlobalSettings().GetAxisSystem().GetUpVector(dir); // this will now return the equivalent of FbxAxisSystem::eZAxis
}
FbxNode* CreateBoneNode(FbxManager* pManger, const char* pName)
{
	return nullptr;
}
FbxNode* CreatePlane(FbxManager* pManager, const char* pName, ModelPart part)
{
	FbxMesh* lMesh = FbxMesh::Create(pManager, pName);
	FbxNode* lNode = FbxNode::Create(pManager, pName);

	lNode->SetNodeAttribute(lMesh);
	lNode->SetShadingMode(FbxNode::eTextureShading);
	lNode->LclRotation.Set(FbxVector4(-90, 0, 0));


	Vertex* vertices = part.GetVertices();
	lMesh->InitControlPoints(part.GetVertSize());
	FbxVector4* lControlPoints = lMesh->GetControlPoints();

	FbxGeometryElementUV* lUVDiffuseElement = nullptr;
	FbxGeometryElementUV* lUVOneElement = nullptr;
	FbxGeometryElementUV* lUVTwoElement = nullptr;
	FbxGeometryElementUV* lUVOMElement = nullptr;

	FbxLayer* Layer = lMesh->GetLayer(0);
	if (Layer == nullptr)
	{
		lMesh->CreateLayer();
		Layer = lMesh->GetLayer(0);
	}

	for (size_t i = 0; i < part.GetVertSize(); i++)
	{
		lControlPoints[i] = ConvertVector3(vertices[i].position);
	}

	// We want to have one normal for each vertex (or control point),
	// so we set the mapping mode to eByControlPoint.
	if (part.HasVertexFlag(VertexFlags::Normals))
	{
		// Create the element.
		FbxLayerElementNormal* LayerElementNormal = FbxLayerElementNormal::Create(lMesh, "");
		LayerElementNormal->SetMappingMode(FbxGeometryElement::eByControlPoint);
		LayerElementNormal->SetReferenceMode(FbxGeometryElement::eDirect);

		// Get the direct array and begin storing our normal vectors into this array.
		FbxLayerElementArrayTemplate<FbxVector4>& DirectArray = LayerElementNormal->GetDirectArray();

		for (size_t i = 0; i < part.GetVertSize(); i++)
		{
			DirectArray.Add(ConvertVector3(vertices[i].normals));
		}

		// We finish normals off by adding it to the layer.
		Layer->SetNormals(LayerElementNormal);
	}
	if (part.HasVertexFlag(VertexFlags::Tangent))
	{
		// Create the element.
		FbxGeometryElementTangent* LayerElementTangent = FbxGeometryElementTangent::Create(lMesh, "");
		LayerElementTangent->SetMappingMode(FbxGeometryElement::eByControlPoint);
		LayerElementTangent->SetReferenceMode(FbxGeometryElement::eDirect);

		// Get the direct array and begin storing our normal vectors into this array.
		FbxLayerElementArrayTemplate<FbxVector4>& DirectArray = LayerElementTangent->GetDirectArray();

		for (size_t i = 0; i < part.GetVertSize(); i++)
		{
			DirectArray.Add(ConvertVector3(vertices[i].tangent));
		}

		// We finish normals off by adding it to the layer.
		Layer->SetTangents(LayerElementTangent);
	}
	if (part.HasVertexFlag(VertexFlags::TexCoords0))
	{
		lUVDiffuseElement = CreateUVElement(lMesh, "DiffuseUV", part);
		CreateMaterialElement(lMesh, "Diffuse Mapping", part);
	}
	if (part.HasVertexFlag(VertexFlags::TexCoords1))
	{
		lUVOneElement = CreateUVElement(lMesh, "UV1", part);
		CreateMaterialElement(lMesh, "UV1 Mapping", part);
	}
	if (part.HasVertexFlag(VertexFlags::TexCoords2))
	{
		lUVTwoElement = CreateUVElement(lMesh, "UV2", part);
		CreateMaterialElement(lMesh, "UV2 Mapping", part);
	}
	if (part.HasVertexFlag(VertexFlags::ShadowTexture))
	{
		lUVOMElement = CreateUVElement(lMesh, "OMUV", part);
		CreateMaterialElement(lMesh, "AO/OM Mapping", part);
	}

	int count3 = lMesh->GetElementVertexColorCount();

	if (part.HasVertexFlag(VertexFlags::Color))
	{
		CreateVertexColor(lMesh, "ColorMap0", part);
	}

	if (part.HasVertexFlag(VertexFlags::Color1))
	{
		CreateVertexColor(lMesh, "ColorMap1", part);
	}

	for (int i = 0; i < part.GetSubMeshCount(); i++)
	{
		SubMesh sub = part.GetSubMeshes()[i];
		lNode->AddMaterial(CreateMaterial(pManager, sub.GetMatName().c_str()));

		for (int x = sub.GetStartIndex()/3; x < ((sub.GetStartIndex()/3) + sub.GetNumFaces()); x++)
		{
			Int3 triangle = part.GetIndices()[x];
			lMesh->BeginPolygon(i);
			lMesh->AddPolygon(triangle.i1);
			lMesh->AddPolygon(triangle.i2);
			lMesh->AddPolygon(triangle.i3);
			lMesh->EndPolygon();

			if (part.HasVertexFlag(VertexFlags::TexCoords0))
			{
				lUVDiffuseElement->GetIndexArray().SetAt(x, i);
			}
			if (part.HasVertexFlag(VertexFlags::TexCoords1))
			{
				lUVOneElement->GetIndexArray().SetAt(x, 0);
			}
			if (part.HasVertexFlag(VertexFlags::TexCoords2))
			{
				lUVTwoElement->GetIndexArray().SetAt(x, 0);
			}
			if (part.HasVertexFlag(VertexFlags::ShadowTexture))
			{
				lUVOMElement->GetIndexArray().SetAt(x, 0);
			}
		}
	}
	int count = lMesh->GetLayerCount();
	// return the FbxNode
	return lNode;
}


// Create a texture
FbxTexture*  CreateTexture(FbxManager* pManager, const char* pName)
{
	FbxFileTexture* lTexture = FbxFileTexture::Create(pManager, pName);

	// Resource file must be in the application's directory.
	FbxString lPath = FbxGetApplicationDirectory();
	FbxString lTexPath = pName;

	// Set texture properties.
	lTexture->SetFileName(lTexPath.Buffer());
	lTexture->SetName(pName);
	lTexture->SetTextureUse(FbxTexture::eStandard);
	lTexture->SetMappingType(FbxTexture::eUV);
	lTexture->SetMaterialUse(FbxFileTexture::eModelMaterial);
	lTexture->SetSwapUV(false);
	lTexture->SetAlphaSource(FbxTexture::eNone);
	lTexture->SetTranslation(0.0, 0.0);
	lTexture->SetScale(1.0, 1.0);
	lTexture->SetRotation(0.0, 0.0);

	return lTexture;
}

// Create material.
// FBX scene must connect materials FbxNode, otherwise materials will not be exported.
// FBX document don't need connect materials to FbxNode, it can export standalone materials.
FbxSurfacePhong* CreateMaterial(FbxManager* pManager, const char* pName)
{
	FbxString lMaterialName = pName;
	FbxString lShadingName = "Phong";
	FbxDouble3 lBlack(0.0, 0.0, 0.0);
	FbxDouble3 lRed(1.0, 0.0, 0.0);
	FbxDouble3 lDiffuseColor(0.75, 0.75, 0.0);
	FbxSurfacePhong* lMaterial = FbxSurfacePhong::Create(pManager, lMaterialName.Buffer());

	// Generate primary and secondary colors.
	lMaterial->Emissive.Set(lBlack);
	lMaterial->Ambient.Set(lRed);
	lMaterial->AmbientFactor.Set(1.);
	// Add texture for diffuse channel
	lMaterial->Diffuse.ConnectSrcObject(CreateTexture(pManager, pName));
	lMaterial->DiffuseFactor.Set(1.);
	lMaterial->TransparencyFactor.Set(0.4);
	lMaterial->ShadingModel.Set(lShadingName);
	lMaterial->Shininess.Set(0.5);
	lMaterial->Specular.Set(lBlack);
	lMaterial->SpecularFactor.Set(0.3);

	return lMaterial;
}

FbxGeometryElementVertexColor* CreateVertexColor(FbxMesh* lMesh, const char* pName, ModelPart& pModel)
{
	FbxGeometryElementVertexColor* element = lMesh->CreateElementVertexColor();
	element->SetMappingMode(FbxLayerElement::eByPolygonVertex);
	element->SetReferenceMode(FbxLayerElement::eIndexToDirect);

	bool color1 = false;

	// We should check if we have Color1 vertex flag to perform this action.
	// Otherwise we'll have very big problems!
	if (strcmp(pName, "ColorMap1") == 0 && pModel.HasVertexFlag(VertexFlags::Color1))
	{
		color1 = true;
	}

	const Vertex* vertices = pModel.GetVertices();
	const std::vector<Int3> indices = pModel.GetIndices();
	const size_t indicesSize = pModel.GetIndicesSize();
	const size_t verticesSize = pModel.GetVertSize();

	FbxLayerElementArrayTemplate<FbxColor>& colorArray = element->GetDirectArray();

	for (size_t i = 0; i < indices.size(); i++)
	{
		size_t verticeIndex = indices[i].i1;
		const unsigned char* vc = (color1 == true ? vertices[verticeIndex].color1 : vertices[verticeIndex].color0);
		FbxColor colour = { vc[0] / 255.0f, vc[1] / 255.0f, vc[2] / 255.0f, vc[3] / 255.0f };
		colorArray.Add(colour);

		verticeIndex = indices[i].i2;
		vc = (color1 == true ? vertices[verticeIndex].color1 : vertices[verticeIndex].color0);
		colour = FbxColor(vc[0] / 255.0f, vc[1] / 255.0f, vc[2] / 255.0f, vc[3] / 255.0f);
		colorArray.Add(colour);

		verticeIndex = indices[i].i3;
		vc = (color1 == true ? vertices[verticeIndex].color1 : vertices[verticeIndex].color0);
		colour = FbxColor(vc[0] / 255.0f, vc[1] / 255.0f, vc[2] / 255.0f, vc[3] / 255.0f);
		colorArray.Add(colour);
	}

	element->GetIndexArray().SetCount(indicesSize);

	for (size_t i = 0; i < indicesSize; i++)
	{
		element->GetIndexArray().SetAt(i, i);
	}

	return element;
}

FbxGeometryElementUV* CreateUVElement(FbxMesh* pMesh, const char* pName, ModelPart& pModel)
{
	// Create UV for Diffuse channel.
	FbxGeometryElementUV* element = pMesh->CreateElementUV(pName);
	FBX_ASSERT(element != nullptr);
	element->SetMappingMode(FbxGeometryElement::eByControlPoint);
	element->SetReferenceMode(FbxGeometryElement::eDirect);
	byte index = 0;
	if (strcmp("DiffuseUV", pName) == 0)
	{
		index = 0;
	}
	if (strcmp("UV1", pName) == 0)
	{
		index = 1;
	}
	if (strcmp("UV2", pName) == 0)
	{
		index = 2;
	}	
	if (strcmp("OMUV", pName) == 0)
	{
		index = 3;
	}
	for (size_t i = 0; i < pModel.GetVertSize(); i++)
	{
		switch (index)
		{
		case 0:
			element->GetDirectArray().Add(FbxVector2(pModel.GetVertices()[i].uv0.x, pModel.GetVertices()[i].uv0.y));
			break;
		case 1:
			element->GetDirectArray().Add(FbxVector2(pModel.GetVertices()[i].uv1.x, pModel.GetVertices()[i].uv1.y));
			break;
		case 2:
			element->GetDirectArray().Add(FbxVector2(pModel.GetVertices()[i].uv2.x, pModel.GetVertices()[i].uv2.y));
			break;
		case 3:
			element->GetDirectArray().Add(FbxVector2(pModel.GetVertices()[i].uv3.x, pModel.GetVertices()[i].uv3.y));
			break;
		}
	}
	element->GetIndexArray().SetCount(pModel.GetIndicesSize());
	return element;
}

FbxGeometryElementMaterial* CreateMaterialElement(FbxMesh* pMesh, const char* pName, ModelPart& pModel)
{
	FbxGeometryElementMaterial* element = pMesh->CreateElementMaterial();
	element->SetName(pName);
	element->SetMappingMode(FbxGeometryElement::eByPolygon);
	element->SetReferenceMode(FbxGeometryElement::eIndexToDirect);
	element->GetIndexArray().SetCount(pMesh->GetPolygonCount());
	return element;
}